const fs=require('fs'),path=require('path'),assert=require('assert/strict'),{pathToFileURL}=require('url');
const root=path.resolve(__dirname,'../..');let chromium;
for(const dir of fs.readdirSync(path.join(process.env.LOCALAPPDATA,'npm-cache/_npx'))){try{chromium=require(path.join(process.env.LOCALAPPDATA,'npm-cache/_npx',dir,'node_modules/playwright')).chromium;break;}catch{}}
(async()=>{const browser=await chromium.launch();try{const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto(pathToFileURL(path.join(root,'AssetDownloads/index.html')).href);
const packs=JSON.parse(fs.readFileSync(path.join(root,'AssetDownloads/_catalog/packs.json'),'utf8'));
assert.equal(await page.locator('.card').count(),packs.length);await page.locator('#query').fill('mini');assert.equal(await page.locator('.card').count(),packs.filter(p=>p.slug.includes('mini')).length);
await page.locator('#query').fill('zz-no-assets-match-zz');assert.equal(await page.locator('.card').count(),0);
await page.locator('#query').fill('');await page.selectOption('#view','files');assert.equal(await page.locator('#packs').isVisible(),false);assert.equal(await page.locator('tbody tr').count(),100);await page.locator('#more').click();assert.equal(await page.locator('tbody tr').count(),300);
await page.locator('#query').fill('gemini-ten');assert.equal(await page.locator('tbody tr').count(),11);await page.selectOption('#category','3D 모델');assert.equal(await page.locator('tbody tr').count(),0);
await page.selectOption('#category','');await page.selectOption('#view','packs');await page.locator('#query').fill('mini');
for(const width of [1600,768,390]){await page.setViewportSize({width,height:1000});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);}
await page.setViewportSize({width:1600,height:1000});await page.locator('#query').fill('');await page.locator('.card img').evaluateAll(async imgs=>{for(const img of imgs){img.loading='eager';await img.decode();}});
await page.screenshot({path:path.join(root,'AssetDownloads/_catalog/preview.png')});assert.deepEqual(errors,[]);
const report={passed:true,packs:packs.length,fileMode:true,search:true,filters:true,pagination:true,previewImagesDecoded:packs.length,mobileOverflow:false,consoleErrors:errors};fs.writeFileSync(path.join(root,'AssetDownloads/_catalog/browser-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
