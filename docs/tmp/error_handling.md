# Error handling

There is a planty of possible errors in this project, and it is important to handle them correctly. C# allows programmer to create custom exceptions, and this mechanism should be widely used in the chat system.

Which custom exception must have its strict message and additional informations, which will allow to determine the reason of error.

## Error groups

Logically, all errors can be be separated to one of few groups:

* System errors: this kind of error is rare, but possible. They appear in case of resourse allocation failure. For example, in case of lack of memory. It is not neccessary to create any custom exceptions for them: built-in excetions will be enough. If such error will appear, the whole program must be shatted down safely.
* **IPK24-CHAT** protocol errors: these errors are specific for the specified protocol. They can be caused by incorrect format of message fields, unexpected type of message, and so on. To handle these errors, special custom exceptions must be created. These esceptions must contain understandable message with additional data (Which types of messages were expected? In which field of message incorrec format was detected?). It is important to mention that many of these errors are not crutual: thay can cause error message sending, but will not terminate the program.
* Inner errors: these errors means that some part of code was written bad. Inattantion or typos can cause them. Custom exceptions for these errors must show location, where thr problem is, and terminate the program execution.

## Implementation

Where is not enough time for very detailed error handling implementation, so there will be just two two castom exception classes:

1) **IpkProtocoleException**
2) **ApplicationErrorException**