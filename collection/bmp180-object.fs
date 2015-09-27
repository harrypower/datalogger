\ This Gforth code reads BMP180 pressure temperature sensor via i2c on BBB rev c hardware
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


require BBB_I2C_lib.fs
require ../BBB_Gforth_gpio/BBB_I2C_lib.fs

object class
    cell% inst-var ac1  \ signed
    cell% inst-var ac2  \ signed
    cell% inst-var ac3  \ signed
    cell% inst-var ac4  \ unsigned
    cell% inst-var ac5  \ unsigned
    cell% inst-var ac6  \ unsigned
    cell% inst-var b1   \ signed
    cell% inst-var b2   \ signed
    cell% inst-var mb   \ signed
    cell% inst-var mc   \ signed
    cell% inst-var md   \ signed
    cell% inst-value i2c-handle
    cell% inst-var buff \ used as a 3 byte buffer



end-class bmp180-i2c
