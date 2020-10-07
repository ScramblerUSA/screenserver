# ScreenShotServer

An ugly quick hackjob of creating a client-server ecosystem for taking and remotely viewing screenshots from other computers.

Installation instructions:
- For client:
  * It is highly desirable that your kid's account doesn't have admin permissions on the machine.
  * Copy the .exe to some legit place (I used c:\program files\common\microsoft shared) - not only will it make the file protected by requiring admin permissions to delete, but will make it less suspicious as well.
  * Open the Windows Explorer and navigate to shell:startup
  * Put a shortcut to the executable so it starts up automatically upon every logon.
- For server:
  * Hosting on a windows machine: just run the server .exe as administrator
  * Hosting on a linux machine: follow the guide at https://github.com/NancyFx/Nancy/wiki/Hosting-Nancy-with-Nginx-on-Ubuntu#create-nancy-website
  
