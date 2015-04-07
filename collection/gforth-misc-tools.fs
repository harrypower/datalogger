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

\ This code is miscellaneous tools for Gforth

require string.fs
require stringobj.fs

string heap-new constant path$
s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ !$
\ this file is setup with configure to conatain the full path

\ variable path$
\ s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ $!  \ configure sets this up to contain the full path

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: #to$ ( n -- c-addr u1 ) \ convert n to string 
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

create floatoutputbuffer
10 allot 
: fto$ ( f: r -- ) ( -- caddr u ) \ convert r from float stack to string
    floatoutputbuffer 10 4 0 f>buf-rdp floatoutputbuffer 10 ;

: nd>fto$ ( f: r -- ) ( nd -- caddr u ) \ convert r from float stack to string with nd digits after deciaml
    floatoutputbuffer 10 rot 0 f>buf-rdp floatoutputbuffer 10 ;

variable mytemppad$
: #to$, ( n -- caddr u ) \ convert n to string then add a "," at the end of the converted string
    #to$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

: dto$, ( d -- caddr u ) \ convert d to string then add a "," at the end of the converted string
    dto$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

: fto$, ( f: r -- caddr u ) \ convert r from floating stack to string with a ',' added
    fto$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

: datetime ( -- ntime ) \ get time from utime but return it as a truncated cell
    utime 1000000 fm/mod swap drop ;

: datetime$ ( -- caddr u ) \ create the current time value of unixepoch and make into a string with a "," at the end of string
    utime 1000000 fm/mod swap drop
    #to$, ;

: init$ ( addr -- ) >r r@ off s" " r> $! ;  \ this is the same as $init in higher version of gforth.
\ use this to initalize a string variable before accessing the string in the variable

: iter$ ( $addr char xt --  )  
    \ this is from gforth verson 0.7.0 $iter but with out the bug
    0 0 { char xt caddr u }
    $@ BEGIN   ( caddr1 u1 -- )
	dup    ( caddr1 u1 u1 -- )
    WHILE      ( caddr1 u1 -- ) 
	    char $split to u to caddr xt execute caddr u
    REPEAT  2drop ;



