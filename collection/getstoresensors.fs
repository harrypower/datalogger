#! /usr/local/bin/gforth
\ the above line works on 0.7.3 gforth and up
\ #! /usr/bin/gforth
\ version 0.7.0 has the /local removed from the path to work

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
require db-stuff.fs

string heap-new constant shlast$
string heap-new constant junk$

5 60 * 1000 * constant rtimes \ the time between readings in ms

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

htu21d-i2c heap-new constant myhtu21d
bmp180-i2c heap-new constant mybmp180

\ this is a test word for hty21d-i2c read-temp-humd object
\ : read-temp-humd ( naddr -- nt nh nflag ) drop 233 200 -20 ;
\ : read-temp-pressure ( naddr -- nt np nflag ) drop 233 88000 -5 ;

: read-thp ( npress nflag ) ( -- F: ftemp F: fhumd ) \ read temperature humidity and pressure
    \ nflag is false for no errors
    \ nflag is non false for the first error generated in this code
    \ note the errors are logged into the database at code exit already
    myhtu21d read-temp-humd dup 0 { nflag1 nflag2 } false =
    if
	swap s>d d>f 10e f/ s>d d>f 10e f/
    else
	nflag1 error!  \ system error about sensor problem
	ht-err error!  \ sensor error 
	2drop 0.0e 0.0e
    then
    mybmp180 read-temp-pressure dup to nflag2 false =
    if
	swap drop  
    else
	nflag2 error!    \ system error about sensors problem
	press-err error! \ sensor error
	2drop 0  
    then
    nflag1 nflag2 or false =
    if
	false
    else
	nflag1 false =
	if nflag2 else nflag1 then
    then ;

string heap-new constant co2cmd$
s" node " co2cmd$ !$
path$ @$ co2cmd$ !+$
s" /collection/get-co2.js" co2cmd$ !+$ 
: read-co2 ( -- nflag ) ( -- F: fco2 ) \ read co2 value
    \ nflag is false if co2 value is read with no errors
    \ nflag is non false for some reading or system error
    \ note floating stack will contain 0.0e if nflag is non false
    \ note the errors are reported to database at end of this word
    try
	co2cmd$ @$ shgets throw 
	junk$ !$ s" co2: " junk$ split$ true =
	if
	    junk$ !$ 2drop 
	    s\" \n" junk$ split$ true =
	    if
		s" err: undefined" search -rot 2drop true =
		if >float false = if co2-err throw else false then
		else co2-err throw then
	    else
		co2-err throw
	    then
	else
	    co2-err throw
	then 
    restore dup if 0.0e dup co2-err <> if dup error! then co2-err error! then
    endtry ;

string heap-new constant nh3cmd$
s" node " nh3cmd$ !$
path$ @$ nh3cmd$ !+$
s" /collection/get-nh3.js" nh3cmd$ !+$ 
: read-nh3 ( -- nflag ) ( -- F: fco2 ) \ read nh3 value
    \ nflag is false if nh3 value is read with no errors
    \ nflag is non false for some reading or system error
    \ note floating stack will contain 0.0e if nflag is non false
    \ note the errors are reported to database at end of this word
    try
	nh3cmd$ @$ shgets throw 
	junk$ !$ s" nh3: " junk$ split$ true =
	if
	    junk$ !$ 2drop 
	    s\" \n" junk$ split$ true =
	    if
		s" err: undefined" search -rot 2drop true =
		if >float false = if nh3-err throw else false then
		else nh3-err throw then
	    else
		nh3-err throw
	    then
	else
	    nh3-err throw
	then 
    restore dup if 0.0e dup nh3-err <> if dup error! then nh3-err error! then
    endtry ;

: read-all-store ( -- ) \ read all sensors and store data in db
    try
	datetime
	read-thp throw
	read-co2 throw
	read-nh3 throw
	localdata!
	false
    restore drop 
    endtry ;

: main-read-loop ( -- ) \ read store wait loop
    begin
	read-all-store
	rtimes ms 
    again ;

main-read-loop bye