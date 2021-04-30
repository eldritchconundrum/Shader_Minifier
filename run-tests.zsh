#!/bin/zsh

set -e

time ./minifier.sh --preserve-externals -o "" tests/**/*.{frag,glsl} > tests/all-glsl.actual

#time dotnet fsi --quiet --exec src/tests.fs

for i in tests/**/*.expected
do
    diff "$i" <(sed s/_ACTUAL_/_EXPECTED_/ "${i/.expected/.actual}") ||
    echo "FAILED: \# diff $i ${i/.expected/.actual/}"
done
