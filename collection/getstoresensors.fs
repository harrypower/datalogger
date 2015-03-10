\ This Gforth code works on BeagleBone Black to retrieve and store sensor iformation
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

\  This code reads two sensors on i2c2 port and 2 analog sensors.
\  The data is stored into a sqlite3 database.

require gforth-misc-tools.fs
require stringobj.fs
require script.fs
require ../BBB_Gforth_gpio/htu21d-object.fs
require ../BBB_Gforth_gpio/bmp180-object.fs

string heap-new constant shlast$
string heap-new constant junk$

next-exception @ constant gss-errorListStart
s" co2 reading error!"                                      exception constant co2-err
s" nh3 reading error!"                                      exception constant nh3-err
s" Pressure sensor reading error!"                          exception constant press-err
s" Humidity Temperature reading error!"                     exception constant ht-err
s" Error place holder #1"                                   exception constant eholder1
s" Error place holder #2"                                   exception constant eholder2
next-exception @ constant gss-errorListEnd

\ this next word is needed to prevent system defunct processes when using sh-get from script.fs
: shgets ( caddr u -- caddr1 u1 nflag ) \ like shget but will not produce defunct processes
    TRY   \ nflag is false the addr1 u1 is the result string from the sh command
	  \ nflag is true could mean memory was not allocated for this command or sh command failed
	shlast$ !$
	s"  ; echo ' ****'" shlast$ !+$ shlast$ @$ sh-get
	2dup shlast$ !$
	s\"  ****\x0a" search true =
	if
	    swap drop 6 =
	    if
		shlast$ @$ 6 - shlast$ !$ shlast$ @$
	    else
		shlast$ @$
	    then
	else
	    shlast$ @$
	then $?
    RESTORE
    ENDTRY ;

