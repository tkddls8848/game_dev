#!/usr/bin/env node
/* Actual browser interaction checks and reproducible 1600x1000 mock captures. */
const fs=require('fs'), path=require('path'), http=require('http'), assert=require('assert/strict');
const ROOT=path.resolve(__dirname,'../..');
const gallery=path.join(ROOT,'docs/poc-gallery');
const manifest=JSON.parse(fs.readFileSync(path.join(gallery,'revised-concepts.json'),'utf8'));
function chromium(){
  try{return require('playwright').chromium;}catch{}
  const cache=path.join(process.env.LOCALAPPDATA,'npm-cache/_npx');
  for(const dir of fs.readdirSync(cache))try{return require(path.join(cache,dir,'node_modules/playwright')).chromium;}catch{}
  throw Error('Playwright not found. Run npx playwright --version and npx playwright install chromium.');
}
const routes={
  scent:[['sample','time1','sample','time2','sample','deduce-coat'],['sample','time1','sample','time2','sample','deduce-clerk']],
  diplomacy:[['literal','literal','literal'],['soften','soften','soften']],
  phone:[['decline','expand','location','rescue'],['answer','charge','wait','rescue']],
  memory:[['person','act','implant'],['implant']],
  map:[['road','gate','petition','door','approve'],['road','gate','approve']],
  curse:[['audit','talk','renegotiate','grain','grain'],['defer','defer','defer']],
  baton:[['cue2','cue2','cue2','cue2'],['cue0','cue0','cue0','cue0']],
  editor:[['move','return'],['strike','replace','return']],
  apartment:[['listen2','move2'],['listen0','move0']],
  prayer:[['morning','read','gate','dispatch'],['morning','dispatch']]
};
const screenshotSetup={scent:['sample','time1','sample'],diplomacy:[],phone:['decline'],memory:[],map:['road','gate','petition'],curse:['audit','talk'],baton:[],editor:['move'],apartment:['listen0'],prayer:['read']};
const mime={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.json':'application/json; charset=utf-8','.png':'image/png'};
const server=http.createServer((req,res)=>{
  try{
    const file=path.resolve(ROOT,'.'+decodeURIComponent(req.url.split('?')[0]));
    if(!file.startsWith(ROOT+path.sep)){res.writeHead(403).end();return;}
    fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream'}).end(data);});
  }catch{res.writeHead(400).end();}
});

async function action(page,id,kind){
  if(kind==='baton'){
    if(await page.locator('#practice').getAttribute('aria-pressed')!=='true')await page.locator('#practice').click();
    for(let n=0;n<Number(id.slice(3));n++)await page.locator('#tick').click();
    await page.locator('#cue').click();
  }else await page.locator(`[data-action="${id}"]`).first().click();
}
async function widthCheck(page,width){
  await page.setViewportSize({width,height:width===1600?1000:900});
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1);
  assert.equal(overflow,false,'horizontal overflow at '+width);
}

(async()=>{
  let browser;
  const reports=[];
  try{
    await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
    const base='http://127.0.0.1:'+server.address().port;
    browser=await chromium().launch();
    fs.mkdirSync(path.join(gallery,'png'),{recursive:true});
    for(const c of manifest){
      const page=await browser.newPage({viewport:{width:1600,height:1000},reducedMotion:'reduce'});
      const errors=[];
      page.on('pageerror',e=>errors.push(e.message));
      page.on('requestfailed',r=>errors.push(r.url()));
      page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
      const url=base+`/games/${c.slug}/presentation/index.html`;
      await page.goto(url,{waitUntil:'networkidle'});
      await page.waitForSelector('#app[data-ready="true"]');
      assert.equal(await page.locator('h1').textContent(),c.title);
      const graph=JSON.parse(fs.readFileSync(path.join(ROOT,'games',c.slug,'data/graph.json'),'utf8'));
      const stateMap=new Map(graph.nodes.map(n=>[n.id,n]));
      const endings=[];
      for(const route of routes[c.kind]){
        await page.locator('#restart').click();
        let node=stateMap.get(graph.start);
        for(const id of route){
          const expected=node.actions.find(a=>a.id===id);
          assert.ok(expected,`${c.slug}: missing action ${id}`);
          await action(page,id,c.kind);
          assert.equal(await page.locator('#app').getAttribute('data-node'),expected.target,`${c.slug}: browser state differs from C#`);
          node=stateMap.get(expected.target);
        }
        assert.equal(await page.locator('#app').getAttribute('data-terminal'),'true');
        endings.push(await page.locator('#ending-title').textContent());
        assert.equal(endings.at(-1),node.ending);
        await page.locator('#replay').click();
        assert.equal(await page.locator('#app').getAttribute('data-node'),graph.start);
      }
      assert.notEqual(endings[0],endings[1]);
      // Narrow viewports still allow real actions, not just screenshot rendering.
      for(const width of [390,768]){
        await widthCheck(page,width);
        await action(page,routes[c.kind][0][0],c.kind);
        await page.locator('#restart').click();
      }
      await widthCheck(page,1600);
      for(const id of screenshotSetup[c.kind])await action(page,id,c.kind);
      await page.evaluate(()=>document.fonts.ready);
      await page.screenshot({path:path.join(gallery,'png',c.slug+'.png')});
      // Endings must remain readable on mobile too.
      await page.locator('#restart').click();
      await widthCheck(page,390);
      for(const id of routes[c.kind][0])await action(page,id,c.kind);
      assert.equal(await page.locator('#ending-title').isVisible(),true);
      assert.equal(errors.length,0,errors.join('\n'));
      reports.push({slug:c.slug,title:c.title,status:'passed',states:graph.nodes.length,endingStates:graph.nodes.filter(n=>n.terminal).length,
        endingsExercised:endings,viewports:[1600,768,390],consoleErrors:errors.length,screenshot:`png/${c.slug}.png`});
      console.log(`PASS ${c.slug}: ${graph.nodes.length} states, two endings, restart, 3 viewports, PNG`);
      await page.close();
    }
    // Deliberately missing graph must produce a useful error surface.
    const errorPage=await browser.newPage();
    await errorPage.route('**/data/graph.json',r=>r.fulfill({status:404,body:'not found'}));
    await errorPage.goto(base+`/games/${manifest[0].slug}/presentation/index.html`);
    await errorPage.waitForSelector('.load-error');
    assert.ok((await errorPage.locator('.load-error').textContent()).includes('http.server'));
    await errorPage.close();
    const galleryPage=await browser.newPage({viewport:{width:1600,height:1000}});
    await galleryPage.goto(base+'/docs/poc-gallery/revised.html',{waitUntil:'networkidle'});
    assert.equal(await galleryPage.locator('.card').count(),10);
    for(const width of [1600,390])await widthCheck(galleryPage,width);
    await galleryPage.setViewportSize({width:1600,height:1000});
    await galleryPage.screenshot({path:path.join(gallery,'png/revised-gallery.png')});
    // This page is deliberately usable by double-clicking it: no fetch dependency.
    await galleryPage.goto(require('url').pathToFileURL(path.join(gallery,'revised.html')).href);
    assert.equal(await galleryPage.locator('#file-notice').isVisible(),true);
    for(const img of await galleryPage.locator('.preview img').all()){
      await img.scrollIntoViewIfNeeded();
      await img.evaluate(el=>el.decode());
      assert.ok(await img.evaluate(el=>el.naturalWidth===1600));
    }
    await galleryPage.close();
    fs.writeFileSync(path.join(gallery,'revised-verification.json'),JSON.stringify({checkedAt:new Date().toISOString(),allPassed:true,missingDataErrorVerified:true,fileGalleryVerified:true,reports},null,2)+'\n');
    console.log('All revised PoCs passed. Screenshots saved to docs/poc-gallery/png.');
  }catch(e){console.error(e);process.exitCode=1;}
  finally{if(browser)await browser.close();server.close();}
})();
