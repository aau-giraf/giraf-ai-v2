$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputFile = "speech_$timestamp.wav"
$outputDir = "YOUR_DIRECTORY"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

docker run --rm `
    -v "${PWD}:/audio" `
    -v "${outputDir}:/output" `
    linuxserver/ffmpeg `
    -y `
    -f s16le -ar 24000 -ac 1 `
    -i /audio/speech.pcm `
    -c:a pcm_s16le -f wav `
    "/output/$outputFile"

Write-Host "Saved audio as: $outputDir\$outputFile"