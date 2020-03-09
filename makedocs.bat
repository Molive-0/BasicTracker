@echo off
cd .
doxygen
.\Docs\latex\make.bat
cp .\Docs\latex\refman.pdf .\manual.pdf
@echo on