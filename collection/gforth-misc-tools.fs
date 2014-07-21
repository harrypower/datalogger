\ This Gforth code are miscellaneous tools

\    Copyright (C) 2014  Philip K. Smith

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

require string.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs


variable path$
s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ $!  \ configure sets this up to contain the full path


: error#to$ ( nerror -- caddr u )  \ takes an nerror number and gives the string for that error
    >r sqlmessg error-cell @ 0 <> r@ 1 >= r@ 101 <= and and r> swap 
    if
	dberrmsg drop \ test for a sqlite3 error and return sqlite3 string if the error is from sqlite3
    else
	>stderr Errlink   \ tested with gforth ver 0.7.0 and 0.7.3  
	begin             \ if the nerror does not exist then a null string is returned!
	    @ dup
	while
		2dup cell+ @ =
		if
		    2 cells + count rot drop exit
		then
	repeat
    then ;

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

variable mytemppad$
: #to$, ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    #to$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

: datetime$ ( -- caddr u ) \ create the current time value of unixepoch and make into a string with a "," at the end of string
    utime 1000000 fm/mod swap drop
    #to$, ;

: init$ ( addr -- ) >r r@ off s" " r> $! ;  \ this is the same as $init in higher version of gforth.
\ use this to initalize a string variable before accessing the string in the variable

: iter$ ( .. $addr char xt -- .. ) \ gforth-string string-iter
    \G takes a string apart piece for piece, also with a character as
    \G separator. For each part a passed token will be called. With
    \G this you can take apart arguments -- separated with '&' -- at
    \G ease.
    \ this is from gforth verson 0.7.9 because $iter in lower versions have a bug
    >r >r
    $@ BEGIN  dup  WHILE  r@ $split i' -rot >r >r execute r> r>
	        REPEAT  2drop rdrop rdrop ;

\ ********************************************************************

: (list$)
  does> ( -- addr nindex  )
    dup dup @ -rot cell + @    \ return address of list$ and current index of list$'s
    0 rot 2 cells + ! ;        \ reset iterator value to zero

: (dolist$!) ( caddr u addr -- )
    @ >r r@ @ r@ cell + @ cells cell + resize throw    \ resize past allocated stuff for new string
    r@ !                                               \ store new address of list$
    r@ @ r@  cell + @ cells +    ( caddr u addr -- )   \ calculate offset to store next string
    dup 0 swap !                 ( caddr u addr -- )   \ clear addr contents to use as string
    $!                           ( caddr u addr-- )    \ store string
    r@ cell + @ 1+ r> cell + ! ; ( -- )                \ update index 

: (list$!) \ note resize here will resize 0 alloted address in gforth only (non ans forth)
  does> ( caddr u -- )
    (dolist$!) ;

: (list$@)
  does> ( -- caddr u )
    @ >r
    r@ cell + @ 0 >  \ if list$ is empty just return null string
    if
	r@ @                       \ get string address star
	r@ 2 cells + @ cells + $@  \ return string with index offset added 
	r@ 2 cells + @ 1 + dup     \ add one to iterator next output
	r@ cell + @ >=             \ if iterator next output to large start again at zero
	if
	    drop 0                 \ restart
	then
	r@ 2 cells + !             \ store next iterator value
    else
	0 0
	0 r@ 2 cells + !           \ ensure iterator has zero for starting index value
    then rdrop ;

: (list$off)
  does> ( -- ) \ note this local is used inside the ?do loop and works in gforth (non ans forth)
    @ { this@ }                 \ note used local here because return stack cant be used in do loops
    this@ cell + @ 0 ?do
	this@ @ i cells + $off  \ free the strings
    loop
    this@ @ free throw          \ free the pointers
    0 this@ !                   \ start at begining
    0 this@ cell + !
    0 this@ 2 cells + ! ;       \ zero iterator 

: (list$>$!)
  does> ( addr nindex )
    swap -rot { addr this }
    0 ?do \ this loops from 0 to nindex
	i cells addr + $@ this (dolist$!)
    loop ;

: list$: ( -- ) ( "name" ) \ used to create a dynamic string array handler
    create here latest { addr nt }
    0 , 0 , 0 ,             \ start address for list$, total stored list$'s, interative index for viewing
    nt name>string addr $!  \ temporarily store name of created list$
    s" -$!" addr $+!    \ add -$! to name for next create
    addr $@ nextname    \ set next create name
    (list$)             \ set doer for name handler

    create addr ,       \ store first list$ addr
    nt name>string addr $!
    s" -$@" addr $+!
    addr $@ nextname
    (list$!)            \ set doer for name string storer

    create addr ,       \ store first list$ addr 
    nt name>string addr $!
    s" -$off" addr $+!
    addr $@ nextname
    (list$@)            \ set doer for name string fetch iterator

    create addr ,       \ name-$off store first list$ addr
    nt name>string addr $!
    s" ->$!" addr $+!
    addr $@ nextname

    addr $off           \ reclaim temporary string
    0 addr !            \ reset cell to start the new list$ 
    
    (list$off)          \ set doer for name string reclaimer

    create addr ,
    (list$>$!) ;         \ set doer for name string copier

\ ************************************************************************