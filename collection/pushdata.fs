\ This Gforth code is used to push data from a Beagle bone black sensor
\ to a Beagle bone black server of aggregated data from several sensors

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


require stringobj.fs
require gforth-misc-tools.fs
require script.fs

string heap-new constant encrypted_data$
string heap-new constant passfile$
string heap-new value junky$
string heap-new value data_to_send$
string heap-new constant cmd$
string heap-new constant cmd2$
string heap-new value test$
string heap-new value output$ 

path$ @$ passfile$ !$ s" /collection/passphrase.data" passfile$ !+$
." passfile$: " passfile$ @$ type cr

s" just a test of the idea for encryption!" data_to_send$ !$

s" gpg -c --passphrase-file " cmd$ !$ passfile$ @$ cmd$ !+$
s"  --batch " cmd$ !+$
s" echo " junky$ !$ data_to_send$ @$ junky$ !+$ s"  | " junky$ !+$
cmd$ @$ junky$ !+$

." cmd$: " junky$ @$ type cr
junky$ @$ shget throw encrypted_data$ !$

s" gpg --decrypt --passphrase-file " cmd2$ !$ passfile$ @$ cmd2$ !+$
s"  --batch " cmd2$ !+$
s" echo " test$ !$ encrypted_data$ @$ test$ !+$ s"  | " test$ !+$
cmd2$ @$ test$ !+$

." cmd2$: " test$ @$ type cr
test$ @$ shget throw output$ !$

data_to_send$ @$ type cr
encrypted_data$ @$ type cr
output$ @$ type cr