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

warnings off
require stringobj.fs
require gforth-misc-tools.fs

string heap-new constant encrypted_data$
string heap-new constant passfile$
string heap-new value junky$
string heap-new value data_to_send$
string heap-new value send-file$
string heap-new constant encryptedfile$
string heap-new constant cmd$
string heap-new constant cmd2$
string heap-new constant decryptedfile$

cr
path$ @$ passfile$ !$ s" /collection/passphrase.data" passfile$ !+$
." passfile$: " passfile$ @$ type cr

path$ @$ send-file$ !$ s" /collection/toencrypt.data" send-file$ !+$
." send-file$: " send-file$ @$ type cr

path$ @$ encryptedfile$ !$ s" /collection/encrypted.data" encryptedfile$ !+$
." encryptedfile$: " encryptedfile$ @$ type cr

path$ @$ decryptedfile$ !$ s" /collection/decrypted.data" decryptedfile$ !+$
." *****************" cr

s" just a test of the idea for encryption!" data_to_send$ !$

\ s" mkfifo " junky$ !$ send-file$ @$ junky$ !+$ s"  &" junky$ !+$ junky$ @$ system $? throw 
\ s" mkfifo " junky$ !$ encryptedfile$ @$ junky$ !+$ s"  &" junky$ !+$ junky$ @$ system $? throw

s" echo " cmd$ !$ data_to_send$ @$ cmd$ !+$ s"  > " cmd$ !+$ send-file$ @$ cmd$ !+$ s"  &" cmd$ !+$
." data: " cmd$ @$ type cr
cmd$ @$ system $? ." system error:" . cr

s" gpg --passphrase-file " cmd$ !$ passfile$ @$ cmd$ !+$
s"  --output - " cmd$ !+$ 
s"  --batch --symmetric " cmd$ !+$ send-file$ @$ cmd$ !+$
s"  > " cmd$ !+$ encryptedfile$ @$ cmd$ !+$ 
s"  &" cmd$ !+$
." cmd$: " cmd$ @$ type cr
." **********" cr 
cmd$ @$ system $? ." system error:" . cr

s" gpg --batch --passphrase-file " cmd2$ !$ passfile$ @$ cmd2$ !+$
s"  --output - " cmd2$ !+$ 
s"  --decrypt " cmd2$ !+$ encryptedfile$ @$ cmd2$ !+$
s"  > " cmd2$ !+$ decryptedfile$ @$ cmd2$ !+$
." cmd2$: " cmd2$ @$ type cr
cmd2$ @$ system $? ." system error:" . cr 
." **************" cr

decryptedfile$ @$ r/o open-file throw value dfid
dfid slurp-fid junky$ !$
dfid close-file throw
." decrypted data: " junky$ @$ type cr
." data_to_send$: "  data_to_send$ @$ type cr

bye