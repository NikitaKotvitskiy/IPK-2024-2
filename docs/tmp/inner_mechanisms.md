## Inner mechanisms

Threads are not the only mechanisms required for program functioning. An important part of the system are inner mechanisms. They should be used for message encoding and decoding and format checks.

## Encoding and decoding

There are two types of messages which can be used in this server application: UDP and TCP variants. The same message translated by different protocols will look diferently. So there must be special class for both protocols. This class will contain common attributes and description of methods that must be implemented in their subclasses.

Each type of message (MSG, AUTH etc.) will also have their classes derived from the common message class.

## Format checking

Format checking will be implemented in simple abstract class. This class will contain methods for checking and will return true or false based on the result. 