# Thread logic

Server must be able to communicate with many client simultaneously, so **threads** are plaing crutual role in program architecture. All threads which will be used have something in common:

* Each thread has its strict role.
* There can by multiple threads with the same role.
* Threads can communicate with each-other.
* Some threads depend on others.

From these facts the following conclusions can be made:
1) Each thread has its type.
2) Threads have hierarchy.
3) There must be the minimal set of running threads, which are crutual for server functioning.

## Types of threads

Different threads play different roles. Some threads are listeners, which are listen to messages from new users or from some specific user. Other ones represent a channel, so they get new messages from users' threads connected to the channel and spread them between other users' threads.

The full list of thread types follows:

* ***Main thread*** - this is a static type, so there can be only one thread of this type in the whole system. Main thread is used for inicializing: it starts the minimal set of threads and then waits for shut down command. When it arrives, **Main thread** must clean up all allocated resourses and then finish the program. Cleaning up means that all other threads must be safely terminated. 
* ***Listener thread*** - there can be variable number of threads with this type. Listeners are listening for messages at some specific port. One of them is public, so it accepts messages from new non-authorizied users and creates a new session communication with that user will be conducted through. Other ones belong to a specific session with dynamically allocated port, so they are listening to messages from one specific user. The algorithm is the same for both: accept message and process it. But the mechanism of processing will depend on type of listener.
* ***Channel thread*** - the purpose of this kind of threads is to route new messages to all users which are currently using this channel. It accepts three types of messages: "new user", "user left" and "message". The third one is standart **IPK24-CHAT** MSG message. The first and the second are inner messages which indicate corresponding action.
* ***User thread*** - these threads represent user sessions. They have all information about user stored in their objects, such as display name, ID of the current channel and so on. The purpose is to process messages from the user and generate inner messages for channel threads. 

## Hierarchy of threads

The purpose of thread hierarchy is to manage threads and to keep the whole system in consisten state. Hierarchy means that each thread (child) depends on some other thread (parent). The only thread which does not have its parent is **main thread**. Child thread cannot exist without its parent, so if parent thread is being terminated, it must safely terminate all its children. Termination means that all ports obtained by thread and all inner resources must be released.

## Crutual set of threads

Some threads are temporary, but others are crutual for the system and must be present during the whole running. Among those are **main thread**, default **channel thread**, and public **listener thread**. It some error appeares in one of these threads, the whole system must be safely shutted down.


## C# inmplementation
The described logic can be implemented using object concept. Special **IpkChatThread** class will be the base class for all types of threads. It will implement the common functionality, such as storing information about children, releasing mechanisms etc. Each type of threads exlucing **main thread** will have its own classes derived from **IpkChatThread** class: **IpkChatListener**, **IpkChatChannel** and **IpkChatUser**. Objects of these classes will communicate with each others and ensure right functionality of the whole chat server. 