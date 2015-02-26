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

variable path$
s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ $!  \ configure sets this up to contain the full path

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: #to$ ( n -- c-addr u1 ) \ convert n to string 
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

create floatoutputbuffer
10 allot 
: fto$ ( f: r -- caddr u ) \ convert r from float stack to string
    floatoutputbuffer 10 3 0 f>buf-rdp floatoutputbuffer 10 ;

variable mytemppad$
: #to$, ( n -- caddr u ) \ convert n to string then add a "," at the end of the converted string
    #to$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

: dto$, ( d -- caddr u ) \ convert d to string then add a "," at the end of the converted string
    dto$ mytemppad$ $! s" ," mytemppad$ $+! mytemppad$ $@ ;

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

\ list$: is used as follows:
\ list$: mystrings
\ Now mystrings is a dynamic string handler
\ mystrings will return address and index to stack.  Index is the total strings in array
\ mystrings also resets the iterator
\ s" some string" mystring-$! \ this is the method you used to add a string to the array
\ mystring-$off \ this will free the strings and the pointer addresses
\ mystring-$@ type \ this will type the current string and will iterate through them each time used
\ list$: otherstrings
\ mystring otherstrings->$! \ this would transfer all strings from mystring to otherstrings handler 
\ ************************************************************************

: (bufr$!)
  does> ( caddr u -- caddr1 u1 )
    dup 2swap                         ( caddr u -- sa sa caddr u )
    dup allocate throw                ( sa sa caddr u -- sa sa caddr u anew )
    dup 2swap                         ( sa sa caddr u anew  -- sa sa anew anew caddr u )
    rot swap dup >r move              ( sa sa anew anew caddr u -- sa sa anew  )
    rot dup @ 0 <>                    ( sa sa anew -- sa anew sa f )
    if @ free throw else drop then    ( sa anew sa -- sa anew  )
    over ! dup cell +                 ( sa anew -- sa sa+4 )
    r> swap ! dup @ swap cell + @ ;   ( sa sa+4 -- sa@ sa+4@ )

: bufr$: ( runtime: caddr u -- caddr1 u1 ) \ will take a string from stack then return it back to stack
    ( compiletime: "name" -- ) 
    \ the point of this is it acts as a fifo buffer with only one space
    \ The main use is to put strings into new allocated memory to prevent s" use causing invalid memory errors
    \ and free the old string memory so there is no leaks
    create here 0 , 0 ,
    latest { addr nt }   
    nt name>string 2 + dup allocate throw addr ! 
    dup addr cell + !
    addr @ swap move
    addr cell + @ 2 - addr @ + '$' swap c!
    addr cell + @ 1 - addr @ + '@' swap c!
    addr @ addr cell + @ nextname
    (bufr$!) 
    create addr , 
  does> @ dup @ swap cell + @ ;

\ bufr$: is used as follows:
\ bufr$: mybuffer
\ This will create a buffer called mybuffer
\ s" some strings" mybuffer type
\ This takes the string from the stack and puts it into the buffer and then outputs the new address and count
\ mybuffer$@ type \ this puts the current string in the buffer onto the stack
\ This buffer handles all the memory allocating and freeing so just use it.
\ The point here is when a string is made like s" some thing" it has an unknown lifetime so if you
\ are passing this strings address and count around depending on other code that s" string may not
\ be valid memory at some point so the buffer would prevent problems.
\ Relize the buffer only contains the last thing it has recieved so it should always be valid
\ but it only will contain the last thing you gave it! If you pass the address around for a string
\ in the buffer it will only be valid until a new string is made then it gets freed so do not keep
\ the address rather use the buffer to get the address.
\ ************************************************************************

