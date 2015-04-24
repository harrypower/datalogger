\  This code uses BB-ADC device tree overlay at boot to read AN0 and AN1
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
\
\  The /boot/uEnv.txt file should have the following lines added to us this code:
\
\ ## This will allow reading ADC at p9-39 to p9-33
\ cape_enable=capemgr.enable_partno=BB-ADC
\
\ Now an ADC value shows up with following code:
\ cat /sys/bus/iio/devices/iio:device0/in_voltage0_raw
\
\ Note this will prevent other capes that use the ADC from working
\ namily the bonescript device tree overlay does not work with this! 


require script.fs

: get-co2 ( -- nraw-ain1 nflag ) \ nraw-ain1 is the value from AIN1 or P9-40 on BBB
    try  \ nflag is false if nraw-ain1 is a valid reading
	s" cat /sys/bus/iio/devices/iio:device0/in_voltage1_raw" shget throw
	s>unumber? false = if d>s else -1 throw then
	false
    restore dup false <> if 0 swap then 
    endtry ;

: get-nh3 ( -- nraw-ain1 nflag ) \ nraw-ain1 is the value from AIN0 or P9-39 on BBB
    try  \ nflag is false if nraw-ain1 is a valid reading
	s" cat /sys/bus/iio/devices/iio:device0/in_voltage0_raw" shget throw
	s>unumber? false = if d>s else -1 throw then
	false
    restore dup false <> if 0 swap then 
    endtry ;

