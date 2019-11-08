git pull
devenv -Build "Release|x64" JarvisButlerBot.sln
cd "JarvisButlerBot\bin\x64\Release\"
start "" "JarvisButlerBot.exe"