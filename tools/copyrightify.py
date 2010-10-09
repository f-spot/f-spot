#!/usr/bin/env python

# Small tool to add copyright headers, used in the GPL -> MIT X11 transition.
#
# TODO: Add support for updating the headers
# TODO: Make sure the header isn't counted when figuring out contributors.

import os
import re
import sys
import subprocess

license_block  = "//\n"
license_block += "// @@FILENAME@@\n"
license_block += "//\n"
license_block += "// Author:\n"
license_block += "@@AUTHORS@@"
license_block += "//\n"
license_block += "@@COPYRIGHTS@@"
license_block += "//\n"
license_block += "// Permission is hereby granted, free of charge, to any person obtaining\n"
license_block += "// a copy of this software and associated documentation files (the\n"
license_block += "// \"Software\"), to deal in the Software without restriction, including\n"
license_block += "// without limitation the rights to use, copy, modify, merge, publish,\n"
license_block += "// distribute, sublicense, and/or sell copies of the Software, and to\n"
license_block += "// permit persons to whom the Software is furnished to do so, subject to\n"
license_block += "// the following conditions:\n"
license_block += "//\n"
license_block += "// The above copyright notice and this permission notice shall be\n"
license_block += "// included in all copies or substantial portions of the Software.\n"
license_block += "//\n"
license_block += "// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,\n"
license_block += "// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF\n"
license_block += "// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND\n"
license_block += "// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE\n"
license_block += "// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION\n"
license_block += "// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION\n"
license_block += "// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.\n"
license_block += "//\n"
license_block += "\n"

filename = sys.argv[1]

f = open(filename, 'r')
code = f.read()

# Probe for headers
has_header = code.find("without limitation the rights to use, copy, modify") != -1
has_legacy_header = code.find("See COPYING") != -1

print "## Processing " + filename

if has_legacy_header:
    print "File seems to contain an old header!"
    sys.exit(255)

if has_header:
    print "Header seems to be already in place"
    sys.exit(0)
    
# Gather some information

blame_info = subprocess.Popen(["git", "blame", "--incremental", filename], stdout=subprocess.PIPE).communicate()[0]

current_author_lines = 0
current_author = ""
total_lines = 0

commit_authors = {}
author_counts = {}
author_details = {}
author_years = {}

for line in blame_info.split("\n"):
    if not re.match("[0-9a-f]{40} ", line):
        continue

    [commit, a, b, lines] = line.split(" ")

    if commit == '0000000000000000000000000000000000000000':
        continue
    
    if not commit in commit_authors:
        # Look up author
        author_info = subprocess.Popen(["git", "show", "--pretty=format:%aD!%aN!%ae", commit], stdout=subprocess.PIPE).communicate()[0]
        author_info = author_info.split("\n")[0]
        [date, name, email] = author_info.split("!", 2)
        year = date.split(" ")[3]

        commit_authors[commit] = name

        if not name in author_details:
            author_details[name] = email
        if not name in author_years:
            author_years[name] = {}
        if not name in author_counts:
            author_counts[name] = 0
        author_years[name][year] = 1

    current_author = commit_authors[commit]
    author_counts[current_author] += int(lines)
    total_lines += int(lines)


# Filter out small contributors (< 10% of the file)
cutoff = total_lines / 10

for k, v in author_counts.iteritems():
    if int(v) <= cutoff:
        del author_years[k]
        del author_details[k]

# Create author lines
authors = ''
for k, v in author_details.iteritems():
    authors += "//   " + k + " <" + v + ">\n"

# Create copyright lines
copyrights = ''
min_year = 0
max_year = 0
p = re.compile('(20[0-9]{2})-((20[0-9]{2})-)*', re.VERBOSE)
for k, y in author_years.iteritems():
    last_year = 0
    line = "// Copyright (C) "
    for v in sorted(y.keys()):
        v = int(v)
        if last_year == 0:
            line += str(v)
        elif last_year == v - 1:
            line += "-" + str(v)
        else:
            line += ", " + str(v)
        last_year = v
        if min_year == 0 or v < min_year:
            min_year = v
        if v > max_year:
            max_year = v
    line = p.sub(r'\1-', line)
    line += " " + k + "\n"
    copyrights += line
if min_year == max_year:
    copyrights = "// Copyright (C) " + str(min_year) + " Novell, Inc.\n" + copyrights
else:
    copyrights = "// Copyright (C) " + str(min_year) + "-" + str(max_year) + " Novell, Inc.\n" + copyrights

# Add it
print "Adding header"

license_text = license_block
license_text = license_text.replace("@@FILENAME@@", filename.split("/")[-1])
license_text = license_text.replace("@@AUTHORS@@", authors)
license_text = license_text.replace("@@COPYRIGHTS@@", copyrights)

tmp = open(filename, "w")
tmp.write(license_text+code)
tmp.close()

