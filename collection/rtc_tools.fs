
\ This Gforth code is a Raspberry Pi DTH 11/22 sensor reader
\    Copyright (C) 2013  Philip K. Smith

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
\
\  I am not currently using this code in the data logger but i have used it in other project.
\  There are just some extra words that help using a RTC ds1307 chip on i2c port 2 of REV 2 of raspberry pi hardware
\  These words allow direct reading of the ds1307 chip along with words that are already in my GPIO library that does more low level stuff with ds1307 chip.


\ error 21 real time clock writing failure of some kind.  error should be logged above this error in error.data
\ error 22 real time clock reading failure of somd kind.  error should be logged above this error in error.data
\ error 23 real time clock not running at this moment.
\ error 24 real time clock not set or not working correctly

include ../gpio/ds1307_i2c_lib.fs
include ../string.fs
include errorlogging.fs

false constant pass
variable date$
variable time$
variable junk$ 

: !datetime { nsec nmin nhour ndate nmonth nyear nday -- nflag } \ nflag is false when time set on rtc
    TRY
  nday ndate nmonth nyear !ds1307_date throw
  nsec nmin nhour !ds1307_time throw
  1 20 !ds1307 throw 5 21 !ds1307 throw 142 22 !ds1307 throw \ just some random test values to show clock was writen to by this function  
  pass
    RESTORE { error } error if error error_log 21 error_log drop drop drop drop drop drop drop error else error then
    ENDTRY ;

: @datetime ( -- nsec nmin nhour ndate nmonth nyear nday nflag ) \ nflag is false when time is returned from rtc
    TRY @ds1307_time throw @ds1307_date throw { nday ndate nmonth nyear } ndate nmonth nyear nday pass 
    RESTORE { error } error if error error_log 22 error_log 0 0 0 0 0 0 0 error else error then  
    ENDTRY ;

: isrtcvalid? ( -- nflag ) \ nflag will be pass for valid rtc and true for not valid rtc values 
    TRY
  0 @ds1307_reg throw 128 and 128 = if 23 error_log true throw then \ this ensures the rtc is currently on 
  20 @ds1307_reg throw 1 <> if 24 error_log true throw then \ register 20 needs a 1 in it to mean time was set
  21 @ds1307_reg throw 5 <> if 24 error_log true throw then \ register 21 needs a 5 in it to mean time was set
  22 @ds1307_reg throw 142 <> if 24 error_log true throw then \ register 22 needs a 142 in it to mean time was set
  pass
    RESTORE 
    ENDTRY
;

: u$ ( n -- caddr u1 ) 0 <<# # #s #> #>> ;
 
: !RTCtosystemdatetime ( -- nflag )  \ if true returned there is some rtc failure.  False returned means all ok
    TRY
  isrtcvalid? throw 
  @datetime throw 
  drop u$ date$ $! u$ date$ $+! u$ date$ $+! u$ time$ $! s" :" time$ $+! u$ time$ $+! s" :" time$ $+! u$ time$ $+!   
  s\" sudo date +%Y%m%d -s \"" junk$ $! date$ $@ junk$ $+! s\" \"" junk$ $+! junk$ $@ system
  s\" sudo date +%T -s \"" junk$ $! time$ $@ junk$ $+! s\" \"" junk$ $+! junk$ $@ system
  pass
    RESTORE
    ENDTRY ;

