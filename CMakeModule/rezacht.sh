#!/bin/bash

if [ $# -eq 0 ]; then
    echo "Usage: $0 <filename>"
    exit 1
fi

filename="$1"
output_file_c="generated/${filename}.g.c"
output_file_h="generated/${filename}.g.h"

generated_source=$((echo -n "generate {${filename}} " && cat "$filename" && echo -n -e "\0") | nc 10.2.21.251 58994)

mkdir generated

(echo -e "$generated_source" | awk -v RS="4c27e626-5404-40f4-bba1-adcc5a721701" 'NR==2 {print $0}') > "${output_file_c}"
(echo -e "$generated_source" | awk -v RS="4c27e626-5404-40f4-bba1-adcc5a721701" 'NR==1 {print $0}') > "${output_file_h}"

echo "Output files created: ${output_file_c}, ${output_file_h}"