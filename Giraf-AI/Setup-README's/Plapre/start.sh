#!/bin/sh
# Starts plapre-serve with an absolute GPU memory budget (in GiB) instead of a
# fraction, so the same amount is reserved on GPUs of any size.
#
#   PLAPRE_GPU_GB   GiB of VRAM for vLLM to reserve (default 4)
#   PLAPRE_GPU_MEM  optional fixed fraction (0-1); if set it wins over PLAPRE_GPU_GB
set -e

GPU_GB="${PLAPRE_GPU_GB:-4}"

if [ -z "${PLAPRE_GPU_MEM:-}" ]; then
    PLAPRE_GPU_MEM=$(PLAPRE_GPU_GB="$GPU_GB" python -c '
import math, os, sys
import torch

gib = float(os.environ["PLAPRE_GPU_GB"])
total = torch.cuda.get_device_properties(0).total_memory / 2**30
frac = gib / total
if frac > 0.95:
    print("WARNING: %.1f GiB requested but the GPU has %.1f GiB; using 95 %%" % (gib, total), file=sys.stderr)
    frac = 0.95
print(math.ceil(frac * 1000) / 1000)
')
    echo "Plapre GPU budget: ${GPU_GB} GiB -> --gpu-mem ${PLAPRE_GPU_MEM}"
else
    echo "Plapre GPU budget: fixed fraction --gpu-mem ${PLAPRE_GPU_MEM}"
fi

exec plapre-serve --checkpoint syvai/plapre-nano-v2 --gpu-mem "${PLAPRE_GPU_MEM}" --port 8000