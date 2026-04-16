@echo off

start "Server" bin\Content.Server\Content.Server.exe
timeout /t 2 >nul
start "Client" bin\Content.Client\Content.Client.exe