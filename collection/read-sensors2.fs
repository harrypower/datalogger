\ This Gforth code is Beaglebone black sensor reading code
\    Copyright (C) 2015  Philip K. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.

\ Currently this code just runs other tools that get the sensors data.

warnings off

require gforth-misc-tools.fs
require stringobj.fs
require script.fs
require ../BBB_Gforth_gpio/htu21d-object.fs
require ../BBB_Gforth_gpio/bmp180-object.fs

string heap-new constant junk$

strings heap-new constant cmdlist

htu21d-i2c heap-new constant myhtu21d
bmp180-i2c heap-new constant mybmp180

myhtu21d display-th
mybmp180 display-tp cr

s" node " junk$ !$
path$ $@ junk$ !+$
s" /collection/get-co2.js" junk$ !+$
junk$ @$ cmdlist !$X

s" node " junk$ !$ 
path$ $@ junk$ !+$
s" /collection/get-nh3.js" junk$ !+$
junk$ @$ cmdlist !$x

cmdlist @$x shget throw type cr
cmdlist @$x shget throw type cr

bye



