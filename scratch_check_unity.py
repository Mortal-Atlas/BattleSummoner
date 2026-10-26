import subprocess

res = subprocess.run(['netstat', '-ano'], capture_output=True, text=True)
for line in res.stdout.splitlines():
    if ':8080' in line or ':8081' in line or '32592' in line or '34208' in line:
        print(line)
