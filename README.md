# QuickFileHoster
A console app for quick and simple file hosting.

To host a file, type:
Host <Port> <IO file path>
Example:
HOST 5000 C:\Users\timeracers\Pictures\hello.jpg

To host a file with a password, type:
Host <Port> <IO file path> <password>
Example:
Host 1234 "C:\Users\timeracers\Pictures\hello.jpg" "password please"

To host multiple files with a password, type:
Host <Port> <IO file path> <password> <IO file path> [<IO file path>] [<IO file path>]...
Example:
host 9001 "C:\Users\timeracers\Pictures\hello.jpg" "same password for all of them" "C:\Users\timeracers\Pictures\greetings.jpg"

To download the first file use a web browser and type the ip address of the hoster plus a colon and the port number.
If you want to download a password protected one then use a tool like postman and add a header for Authorization with the password.
If you want to download any other file then add "/" and the index (0 based indexing) to the url.
