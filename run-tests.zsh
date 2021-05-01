#!/bin/zsh

set -e

test ! -d tests-output/ && rm -rf tests-output/ && cp -r tests tests-output

echo
echo --- Single-process test...
time ./minifier.sh --preserve-externals -o "" tests-output/**/*.{frag,glsl} > tests-output/all-glsl.actual

diff tests-output/all-glsl.expected tests-output/all-glsl.actual ||
    echo "FAILED: \# diff $i ${i/.expected/.actual}"


echo
echo --- Checker test...
time ./checker.sh


if true; then
   echo
   echo --- Multi-process test...
   time dotnet fsi --quiet --exec src/tests.fs

   for i in tests-output/**/*.expected
   do
       diff "$i" <(sed s/_ACTUAL_/_EXPECTED_/ "${i/.expected/.actual}") ||
	   echo "FAILED: \# diff $i ${i/.expected/.actual}"
   done
fi
