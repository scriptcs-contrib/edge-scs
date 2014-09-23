SETLOCAL
SET EnableNuGetPackageRestore=true
node build.js
copy src\edge-scs\bin\Release\*.* lib\ /y