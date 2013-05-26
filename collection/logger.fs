#! /usr/bin/gforth
\ The above line makes this file execute gforth that then compiles and execute the forth code below!  ( note needs to run as root )

\ error 100 -- The dth11 sensor has failed to be read after dth11_trys or 20 times

include ../gpio/rpi_GPIO_lib.fs
include ../string.fs
include errorlogging.fs
include script.fs

0 value fileid
false constant pass
100 constant dth11_fail


variable filelocation$    \ contains path and file name to logged data
variable datavalid$       \ will contain the final valid data to log but used for processing the stings also
variable last_data_saved$ \ will contain the lasted logged data string but not the time date string 
variable junk$

junk$ $init
last_data_saved$ $init
datavalid$ $init
filelocation$ $init
s" /home/pi/git/datalogging/collection/logged_events.data" filelocation$ $!

: filetest ( caddr u -- nflag )
    s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw s" yes" search swap drop swap drop
    if -1
    else 0
    then
;

: heartbeat ( -- ) piosetup drop  25 pipinsetpulldisable drop 25 pipinoutput drop
    25 pipinhigh drop 5 ms 25 pipinlow drop piocleanup drop ;

: readgpio ( ngpio# -- ngpiovalue nflag )
    TRY piosetup throw dup pipinsetpullup throw dup pipininput throw pad pipinread throw pad @ 1 and pass piocleanup throw
    RESTORE  \ remember the stack is returned to entry point then error # added when an error happens 
    ENDTRY ;

: read_dth11 ( -- nhumd ntemp nflag ) \ true returned for nflag means data is not valid false means humd and temp data is valid
    try
	0 0 0 s" sudo /home/pi/git/datalogging/collection/dth11.fs" shget throw { nflag ntemp nhumd caddr u }
	caddr u s>number? throw d>s to nflag caddr u s"  " search
	if to u 1 + to caddr caddr u s>number? throw d>s to ntemp caddr u s"  " search
	    if swap 1 + swap s>number? throw d>s to nhumd else true throw then
	else true throw
	then
	false
    restore if 0 0 true else nhumd ntemp nflag then
    endtry ;


: dataformat ( -- )
    read_dth11 false = if #to$ junk$ $! #to$ junk$ $+! junk$ $@ datavalid$ $! else last_data_saved$ $@ datavalid$ $! dth11_fail error_log then  ; 

: open_data_store ( --  wior ) \ opens the data file for r/w use
     TRY 
	filelocation$ $@ filetest
	if filelocation$ $@ r/w open-file throw
	else  filelocation$ $@ r/w create-file throw
	then to fileid pass
    RESTORE 
    ENDTRY
;

: close_data_store ( -- wior ) \ flushing and close data file
    TRY fileid flush-file throw fileid close-file throw pass 
    RESTORE
    ENDTRY ;

: log_data ( -- flag ) \ Logs the data into data file then flush data. Updated last_data_saved$.  Note datetime$ gets clobbered.
    TRY fileid file-size throw fileid reposition-file throw
	datetime datavalid$ $@ datetime$ $+! datetime$ $@ fileid write-line throw
	fileid flush-file throw
	datavalid$ $@ last_data_saved$ $! pass 
    RESTORE  
    ENDTRY ;

: main_process ( -- )  \ ***this main loop still needs proper error handling*****
    TRY
	begin
	    dataformat datavalid$ $@ last_data_saved$ $@ compare  0<> if open_data_store throw log_data throw close_data_store throw then
	    \ cr datetime$ $@ type depth .    \ this line is just for testing with ssh open running code remove later
	    \ heartbeat
	    30000 ms  
	again
    RESTORE dup error_log close_data_store drop  
    ENDTRY ;

: main_loop
    begin
	main_process -28 = if true else false then  \ bail if user canceled program 
    until 
;

main_loop

bye
