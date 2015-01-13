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
      m: ( i2c -- )
	  0 i2c_handle ! ;m method reset
    public
      m: ( i2c -- )
	  this reset ;m method reset2
      m: ( i2c -- )
	  5 i2c_handle ! ;m overrides construct
      m: ( i2c -- )
	  i2c_handle @ ;m method seeit
end-class htu21d-i2c
