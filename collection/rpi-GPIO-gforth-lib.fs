#! /usr/bin/gforth

include script.fs


0 value memory_id

: piosetup ( -- nflag ) \ nflag is zero for gpio set up and ready to use
    s" /dev/mem" r/w bin open-file swap to memory_id 
;

: piocleanup ( -- nflag ) \ nflag is zero for gpio closed down ok 
    memory_id close-file 
;

: export7 ( -- )
    s\" sudo echo \"7\" > /sys/class/gpio/export" system $? throw
;
: out7
    s\" sudo echo \"out\" > /sys/class/gpio/gpio7/direction" system $? throw
    s\" sudo echo \"0\" > /sys/class/gpio/gpio7/value" system $? throw
;
: in7 
    s\" sudo echo \"in\" > /sys/class/gpio/gpio7/direction" system $? throw
    s" sudo cat /sys/class/gpio/gpio7/value" system $? throw
;
: unexport7
    s\" sudo echo \"7\" > /sys/class/gpio/unexport" system $? throw 
;

: read-cad
    export7
    out7
    utime
    in7
    utime
    unexport7
    2swap d- d.
;
