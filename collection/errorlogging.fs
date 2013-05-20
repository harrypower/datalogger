include ../string.fs
include script.fs

false constant pass

variable datetime$        \ will contain the current date and time string when datetime is called 
variable error_file$	  \ contains the path to the error.data file
variable junk$

junk$ $init
datetime$ $init
error_file$ $init
s" /home/pi/git/datalogging/collection/error.data" error_file$ $!


: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>>
    junk$ $! s" ," junk$ $+! junk$ $@ ;

: datetime ( -- )  \ string for data time created and placed in datetime$ variable
    time&date
    #to$ datetime$ $! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! ; 

: error_log ( nerror -- )  \ error_file$ contains file name of where nerror will be stored
    TRY
	s" test -e " junk$ $! error_file$ $@ junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw s" yes"  search swap drop swap drop 
	if error_file$ $@ w/o open-file throw 
	else error_file$ $@ w/o create-file throw 
	then { nfileid }
	\ error_file$ $@ w/o open-file if drop error_file$ $@ w/o create-file throw then { nfileid }
	nfileid file-size throw nfileid reposition-file throw
	#to$ nfileid write-file throw datetime datetime$ $@ nfileid write-line throw
	nfileid flush-file throw nfileid close-file throw pass pass
    RESTORE drop drop \ dup if swap drop throw else drop drop then
    ENDTRY ;