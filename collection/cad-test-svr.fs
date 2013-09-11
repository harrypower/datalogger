#! /usr/local/bin/gforth

include script.fs

: start-svr ( -- )
    s" sudo /home/pi/git/datalogger/collection/cadmium-svr.fs &" system $? throw ;

: read-cad ( -- caddr u )
    s" sudo echo read > /var/lib/datalogger-gforth/cadmium_cmd" system $? throw
    s" sudo cat /var/lib/datalogger-gforth/cadmium_value" shget throw 
;

: end-svr ( -- )
    s" sudo echo end > /var/lib/datalogger-gforth/cadmium_cmd" system $? throw ;
