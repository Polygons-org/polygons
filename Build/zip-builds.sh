(cd Build/linux && 7z a ../../Build/polygons-linux.zip "data_Polygons_linuxbsd_x86_64" polygons.pck polygons.x86_64)
(cd Build/linux-arm64 && 7z a ../../Build/polygons-linux-arm64.zip "data_Polygons_linuxbsd_arm64" polygons.pck polygons.arm64)
(cd Build/windows && 7z a ../../Build/polygons-windows.zip "data_Polygons_windows_x86_64" polygons.pck polygons.exe)
(cd Build/mac && 7z a ../../Build/polygons-mac.zip polygons.app)