@echo off
rem Renders EP1-EP4 from EX_render_packed.blend into .\renders\EPn_*\ as PNG sequences.
rem Put this .bat next to EX_render_packed.blend and double-click it.
rem Already-rendered frames are skipped, so re-running resumes after a crash.
rem You can also run it on two PCs/instances at once sharing the same folder.
cd /d "%~dp0"
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% (
  echo Blender 5.2 not found at %BLENDER%
  echo Edit the BLENDER line in this file to point at blender.exe
  pause
  exit /b 1
)
for %%S in (EP1_TownTour EP2_FoodThieves EP3_MartStories EP4_RainbowFinale) do (
  echo ===== %%S =====
  %BLENDER% -b EX_render_packed.blend -S %%S -a
)
echo All episodes done.
pause
