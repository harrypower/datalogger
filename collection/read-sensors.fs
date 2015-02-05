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

variable junk$
variable sudo$
s" sudo " sudo$ $!

strings heap-new constant cmdlist

sudo$ $@ junk$ $!
path$ $@ junk$ $+!
s" /BBB_Gforth_gpio/BMP180_i2c.fs" junk$ $+!
junk$ $@ cmdlist !$x  \ pressure sensor

sudo$ $@ junk$ $!
path$ $@ junk$ $+!
s" /BBB_Gforth_gpio/HTU21D_i2c.fs" junk$ $+!
junk$ $@ cmdlist !$x  \ humidity tempertaure sensor

 sudo$ $@ junk$ $!
 s" node " junk$ $+!
 path$ $@ junk$ $+!
 s" /collection/gas-reading.js" junk$ $+!
 junk$ $@ cmdlist !$x  \ gas sensors

: read+print ( -- )
    cmdlist len$ 0 ?do cmdlist @$x shget throw type s" <br>" type  loop ;

\ read+print
\ bye