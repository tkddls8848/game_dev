"""Sync only Tomorrow Map. Never touches the farm project or editable Blender originals."""
from pathlib import Path
import shutil,json,hashlib
ROOT=Path(__file__).resolve().parents[3];GAME=ROOT/'games/puzzle-tomorrow-map';ASSETS=GAME/'unity/Assets'
def copy(src,dst):dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
for src in (GAME/'presentation').iterdir():
    if src.suffix not in ('.cs','.shader'):continue
    target='Editor' if src.name=='ReferenceMapBuild.cs' else 'Runtime'
    copy(src,ASSETS/target/'ReferenceEdition'/src.name)
copy(ROOT/'AssetDownloads/concepts/curse-ledger/fonts/GowunBatang-Regular.ttf',ASSETS/'Resources/ReferenceCity/Heading.ttf')
copy(ROOT/'AssetDownloads/concepts/the-map-lies/fonts/sunflower-Sunflower-Light.ttf',ASSETS/'Resources/ReferenceCity/Body.ttf')
for src in (ROOT/'AssetDownloads/concepts/curse-ledger/fonts').glob('*OFL*'):copy(src,ASSETS/'ThirdParty'/src.name)
copy(ROOT/'AssetDownloads/concepts/the-map-lies/fonts/sunflower-OFL.txt',ASSETS/'ThirdParty/sunflower-OFL.txt')
for src,name in [('bookFlip1.ogg','paper'),('handleSmallLeather.ogg','edit'),('bookClose.ogg','approve')]:copy(ROOT/'AssetDownloads/farming/sfx'/src,ASSETS/f'Resources/ReferenceCity/{name}.ogg')
copy(ROOT/'AssetDownloads/farming/sfx/License-Kenney-RPGAudio.txt',ASSETS/'ThirdParty/License-Kenney-RPGAudio.txt')
print('Synced Tomorrow Map reference presentation only')
