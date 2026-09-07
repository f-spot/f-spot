#!/bin/bash
set -e

./autogen.sh $1
cd build
make
cd ../lib/
make
cd ..
