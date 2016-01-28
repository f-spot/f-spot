#!/bin/bash

./autogen.sh $1
cd build
make
cd ../lib/
make
cd libfspot
sudo make install
cd ../..
