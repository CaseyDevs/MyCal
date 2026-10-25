#!/usr/bin/env python3
"""Create a private development login. Credentials stay outside Git in .local/."""
import os
from pathlib import Path
import secrets
import subprocess

root = Path(__file__).resolve().parents[1]
private = root / '.local'
private.mkdir(mode=0o700, exist_ok=True)
credentials = private / 'admin-credentials.txt'
if credentials.exists():
    print('Local sign-in details already exist in .local/admin-credentials.txt')
else:
    email = 'casey@mycal.local'
    password = secrets.token_urlsafe(18) + 'aA1!'
    environment = dict(os.environ, ASPNETCORE_ENVIRONMENT='Development',
                       Bootstrap__Email=email, Bootstrap__Password=password)
    subprocess.run(['dotnet', 'bin/Debug/net10.0/MyCal.Web.dll', '--bootstrap-admin'],
                   cwd=root / 'MyCal.Web', env=environment, check=True)
    fd = os.open(credentials, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, 'w') as stream:
        stream.write(f'Local development login only\nEmail: {email}\nPassword: {password}\n')
    print('Local sign-in details saved in .local/admin-credentials.txt')
