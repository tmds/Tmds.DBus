# D-Bus Overview

D-Bus consists of a server daemon and clients connecting to the daemon. There is a system daemon (**system bus**) and there are daemons per user session (**session bus**).

Programs connecting to the bus can provide or consume services. A **service** exposes **objects** at specific **paths**. These objects implement **interfaces**, which contain **methods** (RPC targets) and **signals** (events). For example, the `org.freedesktop.NetworkManager` which lives on the *System bus*, exposes an object at `/org/freedesktop/NetworkManager`. This object implements the `org.freedesktop.DBus.Introspectable` interface to perform reflection, the `org.freedesktop.DBus.Properties` interface to get and set properties and the `org.freedesktop.NetworkManager` interface which has signals like `DeviceAdded` and methods such as `ActivateConnection` and `GetDevices`. This latter method returns an array of object paths pointing to other objects exposed by the service.

When a client connects to the daemon, it is assigned a *unique name*. In case it is a service provider, it will register specific *service names*. A service consumer can query the bus for the registered services. Certain services can also be started by the daemon (*activatable* services). This relies on configuration files which tell the daemon what application will provide a specific service.

The `org.freedesktop.DBus.Introspectable` interface has a single method `Introspect` which returns an XML string that describes the object. It contains the interfaces and their signals, methods and properties. It also includes child nodes which describe objects below it in the object path.

Method arguments are annotated as input or output. Signal arguments are always output parameters. Each argument and property has a type-attribute which contains a *signature* string describing the type of the argument/property.

The following block shows an example from the dbus specification:
```
<!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN"
    "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
<node name="/com/example/sample_object">
    <interface name="com.example.SampleInterface">
    <method name="Frobate">
        <arg name="foo" type="i" direction="in"/>
        <arg name="bar" type="s" direction="out"/>
        <arg name="baz" type="a{us}" direction="out"/>
        <annotation name="org.freedesktop.DBus.Deprecated" value="true"/>
    </method>
    <method name="Bazify">
        <arg name="bar" type="(iiu)" direction="in"/>
        <arg name="bar" type="v" direction="out"/>
    </method>
    <method name="Mogrify">
        <arg name="bar" type="(iiav)" direction="in"/>
    </method>
    <signal name="Changed">
        <arg name="new_value" type="b"/>
    </signal>
    <property name="Bar" type="y" access="readwrite"/>
    </interface>
    <node name="child_of_sample_object"/>
    <node name="another_child_of_sample_object"/>
</node>
```
These primitive types are defined in the specification:

Conventional name | Signature | Description
------------------|-----------|------------
BYTE	          | y         | Unsigned 8-bit integer
BOOLEAN	          | b         |	Boolean value: 0 is false, 1 is true, any other value allowed by the marshalling format is invalid
INT16	          | n         |	Signed (two's complement) 16-bit integer
UINT16	          | q         |	Unsigned 16-bit integer
INT32	          | i         |	Signed (two's complement) 32-bit integer
UINT32	          | u         |	Unsigned 32-bit integer
INT64	          | x         |	Signed (two's complement) 64-bit integer (mnemonic: x and t are the first characters in "sixty" not already used for something more common)
UINT64	          | t         |	Unsigned 64-bit integer
DOUBLE	          | d         |	IEEE 754 double-precision floating point
UNIX_FD	          | h         |	Unsigned 32-bit integer representing an index into an out-of-band array of file descriptors, transferred via some platform-specific mechanism (mnemonic: h for handle)
STRING	          | s         |	No extra constraints
OBJECT_PATH	      | o         |	Must be a syntactically valid object path
SIGNATURE	      | g         |	Zero or more single complete types

It is also possible to create aggregate types called **STRUCTS**. Structs signatures are created by concatenating the members and enclosing them with parentheses. For example `(si)` is a struct containing a string and a 32-bit integer.

**ARRAY** types are described by prefixing a type with 'a'. For example, `ao` is an array of object paths.

**DICTIONARIES** are a special type of array with a key and a value type. Their signature string is `a{<keytype><valuetype>}`. For example `a{is}` is a dictionary which maps 32-bit signed integers to strings.

There is also an any type (**VARIANT**) which can contain one other type. This variant's signature string is `v`.

Combining these rules allows us to interprete a signature found in the introspection XML. For example: `aa{sv}` describes an array (`a`) of dictionaries `a{}` with a string key (`s`) and an variant (`v`) value.

You can learn more about D-Bus at https://www.freedesktop.org/wiki/Software/dbus/.