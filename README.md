# DynamoDbTransactions.Net
Port of https://github.com/awslabs/dynamodb-transactions/ to the .NET Core framework

# WARNING
This code is untested, many of the unit tests have yet to be fixed and the integration tests have never been run.
This was ported during several overnight sessions with a tired and weary mind. This is my first attempt at porting
anything significant from Java to C#. 

# Current State
Stylistically the code still needs some cleaning up but functionally it *should* match the java 
version with the exception of the utilisation of async methods of the AWS DynamoDb SDK.

If anyone wants to help out by fixing the rest of the tests or cleaning things up then please do!


# Disclaimer / Licence
Copyright 2017 Tim Adamson

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR 
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
