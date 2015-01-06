var co2 = require('bonescript');
var nh3 = require('bonescript');

co2.analogRead('P9_40', co2print);
nh3.analogRead('P9_39', nh3print);

function co2print(x) {
    console.log('co2: ' + x.value);
    console.log('co2 reading error: ' + x.err);
}

function nh3print(x) {
    console.log('nh3: ' + x.value);
    console.log('nh3 reading error: ' + x.err);
}