param([string]$text)

@{input=$text; accent="a"; voice="f"} | ConvertTo-Json -Compress | Set-Content request.json -Encoding utf8

curl.exe -s http://localhost:8000/v1/audio/speech -H "Content-Type: application/json" --data-binary "@request.json" -o "speech_$(Get-Date -Format 'yyyyMMdd_HHmmss').wav"