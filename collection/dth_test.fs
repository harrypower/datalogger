#! /usr/bin/gforth

\ error 1000 -- to many transitions have happened to be a proper message from dth device

include ../gpio/rpi_GPIO_lib.fs
include ../string.fs
include script.fs

1000 constant many_transitions_fail

1500 constant max_data_quantity
100 constant max_transitions
max_transitions 1 - constant max_timings
18 constant dth_start_time


25 value gpio_dth_pin
0 value dth_data_location
0 value dth_data_transitions
0 value dth_data_timings
0 value dth_transitions_size
0 value dth_timings_index
11 value dth-11-22?
20 value retry-times

: dth_var_reset ( -- )
    0 to dth_timings_index
    0 to dth_transitions_size ;

: dth_data_storage_setup ( -- ) \ gets the memory for storage of dth reading procedure
    dth_data_location 0=
    if
	max_data_quantity allocate throw to dth_data_location
	max_transitions cell * allocate throw to dth_data_transitions
	max_timings cell * allocate throw to dth_data_timings
    then  ;

: dth_start_signal ( -- ) \ will put dth sensor in sampling mode
    piosetup throw gpio_dth_pin pipinsetpulldisable throw gpio_dth_pin pipinhigh throw
    gpio_dth_pin pipinoutput throw gpio_dth_pin pipinhigh throw gpio_dth_pin pipinlow throw
    dth_start_time ms gpio_dth_pin pipinhigh throw gpio_dth_pin pipininput throw  ;

: dth_shutdown ( -- ) \ clean up gpio mode
    piocleanup throw ;  

: dth_read ( -- nvalue )
    gpio_dth_pin pad pipinread throw pad c@ ;

: dth_get_data ( -- )
    dth_var_reset dth_data_storage_setup
    max_data_quantity 0 dth_start_signal ?do dth_read dth_data_location i + c! loop dth_shutdown ;

: dth_data@ ( nindex -- nvalue ) \ retreaves unprocesed raw dth stored readings
    dth_data_location + c@ ;

: seedata ( nvalue -- ) \ testing word to see nvalue amount of the data starting at location 0
    cr 0 ?do i dth_data@  . s"  " type loop ; 

: transitions! ( nvalue -- ) \ store list of transitions 
    dth_transitions_size cell * dth_data_transitions + !
    dth_transitions_size 1 + to dth_transitions_size
    dth_transitions_size max_transitions > if many_transitions_fail throw then ;

: transitions ( -- ) \ make list of transitions to work with
    0 dth_data@ { nvalue } max_data_quantity 1 ?do i dth_data@ nvalue <> if i transitions! i dth_data@ to nvalue then loop ;

: transitions@ ( nindex -- nvalue ) \ get the transition from list
    cell * dth_data_transitions + @ ;

: seetransitions ( nvalue -- ) \ testing word to see transition list
    cr 0 ?do i transitions@ . s"  " type loop ;

: timings! ( nvalue -- )
    dth_timings_index cell * dth_data_timings + !
    dth_timings_index 1 + to dth_timings_index ;

: timings ( -- )
    0 transitions@ { nvalue } max_transitions 1 ?do i transitions@ nvalue - timings! i transitions@ to nvalue loop ;

: timings@ ( nindex -- nvalue )
    cell * dth_data_timings + @ ;

: seetimings ( nvalue -- ) \ testing word for timings list
    cr 0 ?do i timings@ . s"  " type loop ;


: testit ( -- ) 
    dth_get_data 1500 seedata
    transitions 100 seetransitions
    timings 99 seetimings ;


