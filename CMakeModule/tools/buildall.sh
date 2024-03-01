#!/bin/bash

find ./generated -type f -name "*.g.h" -exec rm -f {} +
find ./generated -type f -name "*.g.c" -exec rm -f {} +

find ./views -type f -name "*.view" -exec ./tools/rezacht.sh {} \;