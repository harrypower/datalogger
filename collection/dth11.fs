#! /usr/bin/gforth

\ error 1001 -- no start indicator for dth11 bit message
\ error 1002 -- code did not see the change from 0 to 1 at bit start message so all data might be faulty
\ error 1003 -- start indicator did not seem to be more then 60 us it needs to be 80 us or it is not the header
\ error 1004 -- second part of header not read corretly 
\ error 1005 -- still issues in second part of header
\ error 1006 -- seems the header is compleatly not valid or not recieved correcly anyways
\ error 1007 -- the start of a bit of data from dth11 seems to not be valid
\ error 1008 -- the bit recieved from dth11 is possibly not correct
\ error 1009 -- this means the data table recieved from dth11 did not contain all the data for correct processing into bytes so it must be corrupted
\ error 1010 -- check sum from dth11 did not match the iterpreded bytes from dth11 so data must be corrupted 
\ error 1011 -- dth11 is currently busy.  some other process is reading this sensor right now!
\ error 1012 -- dth11 did not read after 20 attempts

warnings off

include ../gpio/rpi_GPIO_lib.fs
include ../string.fs
include script.fs

1001 constant no_start_fail
1002 constant no_change_fail
1003 constant <60us_fail
1004 constant header_part_fail
1005 constant more_header_fail
1006 constant invalid_header_fail
1007 constant start_bit_fail
1008 constant dth11_bit_fail
1009 constant data_table_fail
1010 constant checksum_fail
1011 constant dth11_busy_fail
1012 constant 20_trys_fail

variable junk$
variable dth11_self$

0 value dth11_data_location
0 value dth11_size
0 value dth11_packetstart
0 value dth11_readlocation

dth11_self$ $init
junk$ $init

s" dth11.fs" dth11_self$ $!

: dth11_busy? ( -- nflag ) \ true means that another dth11 process is busy with sensor
    s" pgrep -c " junk$ $! dth11_self$ $@ junk$ $+! junk$ $@ shget
    if 2drop false
    else s>number?  if 2drop false else d>s 1 = if false else true then  then
    then
;

: dth11_var_reset 
    0 to dth11_size
    0 to dth11_packetstart
    0 to dth11_readlocation ;

: dth11_data_storage_setup dth11_data_location 0= if 300 cell * allocate throw to dth11_data_location then  ;

: wait ( nduration -- ) \ this duration is 70 us minimum duration up to what ever you want  
    utime rot 70 - s>d d+ begin 2dup utime d<= until 2drop ;

: dth11_start_signal piosetup throw 25 pipinsetpulldisable throw 25 pipinoutput throw 25 pipinhigh throw 25 pipinlow throw 18000 wait 25 pipinhigh throw 25  pipininput throw  ;

: dth11_shutdown piocleanup throw ;

: dth11_read ( -- nvalue ) 25 pad pipinread throw pad c@ ;

: dth11_change ( -- nsvalue ncvalue ntime )
    utime dth11_read rot rot 0 begin drop 2dup utime d- dabs 300 0 d> dth11_read dup 5 pick <> rot or until rot rot utime d- dabs d>s ;


\ : test dth11_start_signal begin dth11_change dup 300 >= until cr depth 3 / 0 ?do . . . cr loop dth11_shutdown ;

: dth11_getdata ( --  )  \ this code stores the dth11 raw timing data into memory starting at dth11_data with size of stored amount being dth11_size
    dth11_start_signal depth { dstack } begin dth11_change dup 300 >= until drop drop drop depth dstack - to dth11_size dth11_shutdown  
    dth11_size 0 ?do dth11_data_location i cell * + ! loop ;

: dth11_data_retrieve ( nindex -- nvalue ) \ not nindex value must be between 0 and dth11_size and no checking is done here for that condition
    cell * dth11_data_location + @ ;

: dth11_correct_start? ( -- nflag ) \ false returned means header is valid so continue 
    dth11_size 1 - dth11_data_retrieve 0 <> if no_start_fail throw then \ do not have start low level message from dth11
    dth11_size 2 - dth11_data_retrieve 1 <> if no_change_fail throw then \ should have seen change to 1 from dth11 here
    dth11_size 3 - dth11_data_retrieve 50 < if <60us_fail throw then \ should have a transition time greater then 60 for this 0 to 1 event
    dth11_size 4 - dth11_data_retrieve 1 <> if header_part_fail throw then \ need a 1 at this time for header to be valid
    dth11_size 5 - dth11_data_retrieve 0 <> if more_header_fail throw then \ need to see 0 for transition
    dth11_size 6 - dth11_data_retrieve 50 < if invalid_header_fail throw then \ header from dth11 is ok if this is more then 60
    dth11_size 7 - to dth11_packetstart \ store the start of the dth11 info packet
    false \ the header is ok if code gets to here
;

: seeit dth11_size dup 10 - ?do i dup . dth11_data_retrieve . cr loop ;

: dth11_getbit ( -- nvalue )
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  0 <> if start_bit_fail throw then  
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  1 <> if start_bit_fail throw then
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  40 < if start_bit_fail throw then
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  1 <> if dth11_bit_fail throw then
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  0 <> if dth11_bit_fail throw then 
    dth11_readlocation dth11_data_retrieve dth11_readlocation 1 - to dth11_readlocation  40 < if 0 else 1 then 
;

: dth11_getbyte ( -- nvalue )
    dth11_readlocation 48 < if data_table_fail throw then 
    8 0 ?do dth11_getbit loop
    swap 2 * +
    swap 4 * +
    swap 8 * +
    swap 16 * +
    swap 32 * +
    swap 64 * +
    swap 128 * + 
;
: dth11_valid_data { nrh nrhd nt ntd nck -- nrh nt nflag }
    nrh nt nrh nrhd + nt + ntd + nck <> ;

: dth11_parse ( -- ntemp nhumd nerror ) \ false returned means that the reading was good ... anything else is bad data
    TRY dth11_var_reset  
	dth11_data_storage_setup
	dth11_getdata dth11_correct_start? throw
	dth11_packetstart to dth11_readlocation
	dth11_getbyte dth11_getbyte dth11_getbyte dth11_getbyte dth11_getbyte 
	dth11_valid_data if checksum_fail throw then
	false
    RESTORE dup if 0 swap 0 swap then 
    ENDTRY
;

: get_temp_humd ( -- ntemp nhumd nerror )
    dth11_busy? false =
    if
	20 { ntimes } 1 2
	begin
	    drop drop
	    ntimes 0 >
	    if
		ntimes 1 - to ntimes
		dth11_parse dup 0 = if true else false 1800 ms then
	    else 0 0 20_trys_fail true
	    then
	until
    else
	0 0 dth11_busy_fail 
    then
    
    . . . 
;

get_temp_humd bye