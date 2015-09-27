\ This Gforth code is an object that reads HTU21D temperature humidity sensor via i2c on BBB rev c hardware
\ This code reads i2c-1 but this should be mapped to i2c-2 device on p9 header.
\ The method of reading the sensor is 'Hold Master' and this means this sensor
\ will hold the i2c data lines until a reading is done!
\
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
require objects.fs
require ../BBB_Gforth_gpio/BBB_I2C_lib.fs

object class
    cell% inst-var i2c_handle
    cell% inst-var htu21d_addr
    cell% inst-var i2c_addr
    cell% inst-var temp_read
    cell% inst-var humd_read
    cell% inst-var abuffer     \ this is used as a 3 byte buffer cell% should allocate 4 bytes on 32 bit system
    protected
    m: ( i2c -- temp )  \ reads and returns temperature on stack and floaging stack
	( f: -- temp )
	i2c_addr @ htu21d_addr @ bbbi2copen dup i2c_handle ! true = throw
	i2c_handle @ temp_read @ bbbi2cwrite-b throw
	i2c_handle @ abuffer 3 bbbi2cread 3 <> throw
	abuffer c@ 8 lshift
	abuffer 1 + c@
	%11111100 and +
	s>d d>f
	65536e f/
	175.72e f*
	-46.85e fswap f+ fdup 10e f*
	f>d d>s
	i2c_handle @ bbbi2cclose true = throw ;m method read-temp

    m: ( i2c -- humd )
	( f: -- humd )
	i2c_addr @ htu21d_addr @ bbbi2copen dup i2c_handle ! true = throw
	i2c_handle @ humd_read @ bbbi2cwrite-b throw
	i2c_handle @ abuffer 3 bbbi2cread 3 <> throw
	abuffer c@ 8 lshift
	abuffer 1 + c@
	%11111100 and +
	s>d d>f
	65536e f/
	125e f*
	-6e fswap f+ fdup 10e f*
	f>d d>s
	i2c_handle @ bbbi2cclose true = throw ;m method read-humd
    public
    m: ( i2c -- temp nflag )
	( f: -- temp )
	this ['] read-temp catch dup 0 <>
	if 0e then ;m method temperature
    m: ( i2c -- humd nflag )
	( f: -- humd )
	this ['] read-humd catch dup 0 <>
	if 0e then ;m method humidity
    m: ( i2c -- ) \ start all values for i2c usage and htu21d config
	0 i2c_handle !
	0x40 htu21d_addr !
	1 i2c_addr !     \ note this is the i2c port address as enumerated by linux on BBB
	\ 1 means i2c-1 but realize this is mapped to i2c-2 device on p9 header of BBB
	0xe3 temp_read ! \ this is message for reading temperature
	0xe5 humd_read ! \ this is message for reading humidity
	0 abuffer ! ;m overrides construct
end-class htu21d-i2c

htu21d-i2c heap-new constant test
test temperature . . f.
test humidity . . f.
