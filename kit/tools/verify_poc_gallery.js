const fs=require('fs'),path=require('path'),assert=require('assert/strict'),{pathToFileURL}=require('url');
const ROOT=path.resolve(__dirname,'../..'),GALLERY=path.join(ROOT,'docs/poc-gallery');
const catalog=JSON.parse(fs.readFileSync(path.join(GALLERY,'catalog.json'),'utf8'));
let chromium;
for(const dir of fs.readdirSync(path.join(process.env.LOCALAPPDATA,'npm-cache/_npx'))){try{chromium=require(path.join(process.env.LOCALAPPDATA,'npm-cache/_npx',dir,'node_modules/playwright')).chromium;break;}catch{}}
if(!chromium)throw Error('Playwright unavailable');
(async()=>{
  const browser=await chromium.launch();
  try{
    const entries=catalog.entries,ids=new Set(entries.map(p=>p.slug));
    assert.equal(ids.size,entries.length);
    const directories=fs.readdirSync(path.join(ROOT,'games'),{withFileTypes:true}).filter(d=>d.isDirectory());
    assert.equal(entries.length,directories.length);
    for(const p of entries){
      for(const key of ['presentation','screenshot','fullScreenshot','readme'])if(p[key])assert.ok(fs.existsSync(path.resolve(GALLERY,p[key])),`${p.slug}: ${key}`);
      if(p.related)assert.equal(entries.find(other=>other.slug===p.related)?.related,p.slug);
      if(p.status==='pending')assert.equal(p.presentation,null);
    }
    const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    await page.goto(pathToFileURL(path.join(GALLERY,'index.html')).href);
    assert.equal(await page.locator('.card').count(),entries.length);
    assert.equal(await page.locator('iframe').count(),0);
    await page.locator('#collection').selectOption('revised');
    assert.equal(await page.locator('.card').count(),entries.filter(p=>p.collection==='revised').length);
    await page.locator('[data-related="the-map-lies"]').click();
    assert.equal(await page.locator('.card').count(),entries.length);
    assert.ok(page.url().endsWith('#the-map-lies'));
    await page.locator('#clear').click();
    await page.locator('#search').fill('definitely-no-such-poc');
    assert.equal(await page.locator('#empty').isVisible(),true);
    await page.locator('#search').fill('deck-');
    assert.equal(await page.locator('.card').count(),entries.filter(p=>`${p.title} ${p.slug} ${p.summary}`.toLowerCase().includes('deck-')).length);
    await page.locator('#clear').click();
    await page.locator('[data-genre="farm"]').click();
    assert.equal(await page.locator('.card').count(),entries.filter(p=>p.genre==='farm').length);
    await page.locator('#clear').click();
    await page.locator('#type').selectOption('pending');
    assert.equal(await page.locator('.open').count(),0);
    await page.locator('#clear').click();
    await page.locator('#sort').selectOption('title');
    assert.deepEqual(await page.locator('.card h2').allTextContents(),entries.map(p=>p.title).sort((a,b)=>a.localeCompare(b,'ko')));
    await page.locator('#clear').click();
    for(const width of [1600,768,390]){
      await page.setViewportSize({width,height:1000});
      assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);
    }
    for(const img of await page.locator('.preview img').all()){
      await img.scrollIntoViewIfNeeded();await img.evaluate(el=>el.decode());
      assert.ok(await img.evaluate(el=>el.naturalWidth>0));
    }
    await page.setViewportSize({width:1600,height:1000});await page.evaluate(()=>scrollTo(0,0));
    await page.screenshot({path:path.join(GALLERY,'png/catalog-overview.png')});
    await page.goto(pathToFileURL(path.join(GALLERY,'shots.html')).href);
    assert.equal(await page.locator('.card').count(),entries.filter(p=>p.screenshot).length);
    assert.equal(errors.length,0,errors.join('\n'));
    const result={passed:true,projects:entries.length,screenshots:entries.filter(p=>p.screenshot).length,relatedPairs:entries.filter(p=>p.related).length/2,
      checks:['catalog completeness','local links','reciprocal variants','search','filters','sorting','related navigation','empty state','file mode','PNG decode','3 viewport widths','no automatic iframes'],consoleErrors:0};
    fs.writeFileSync(path.join(GALLERY,'gallery-verification.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify(result));
  }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
