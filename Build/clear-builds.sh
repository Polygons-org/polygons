rm -rf Build/linux/*
rm -rf Build/windows/*
rm -rf Build/mac/*
rm -rf Build/linux-arm/*

touch Build/linux/.gitkeep
touch Build/windows/.gitkeep
touch Build/mac/.gitkeep
touch Build/linux-arm/.gitkeep

rm -rf Build/polygons-linux-arm.zip
rm -rf Build/polygons-linux.zip
rm -rf Build/polygons-windows.zip
rm -rf Build/polygons-mac.zip