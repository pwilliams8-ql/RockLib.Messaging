﻿using System;
using System.Net.Mime;

namespace RockLib.Messaging.CloudEvents
{
    /// <summary>
    /// The base type for cloud events.
    /// </summary>
    public abstract class CloudEvent
    {
        /// <summary>The name of the <see cref="Id"/> header.</summary>
        public const string IdHeader = "id";

        /// <summary>The name of the <see cref="Source"/> header.</summary>
        public const string SourceHeader = "source";

        /// <summary>The name of the <see cref="SpecVersion"/> header.</summary>
        public const string SpecVersionHeader = "specversion";

        /// <summary>The name of the <see cref="Type"/> header.</summary>
        public const string TypeHeader = "type";

        /// <summary>The name of the <see cref="DataContentType"/> header.</summary>
        public const string DataContentTypeHeader = "datacontenttype";

        /// <summary>The name of the <see cref="DataSchema"/> header.</summary>
        public const string DataSchemaHeader = "dataschema";

        /// <summary>The name of the <see cref="Subject"/> header.</summary>
        public const string SubjectHeader = "subject";

        /// <summary>The name of the <see cref="Time"/> header.</summary>
        public const string TimeHeader = "time";

        private static IProtocolBinding _defaultProtocolBinding = ProtocolBinding.Default;

        /// <summary>
        /// Gets or sets the default <see cref="IProtocolBinding"/>. This is used when one a
        /// <see cref="IProtocolBinding"/> is required by a method but was not provided
        /// (i.e. passed as <see langword="null"/>) by the caller.
        /// </summary>
        public static IProtocolBinding DefaultProtocolBinding
        {
            get => _defaultProtocolBinding;
            set => _defaultProtocolBinding = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// REQUIRED. Identifies the event. Producers MUST ensure that source + id is unique for each
        /// distinct event. If a duplicate event is re-sent (e.g. due to a network error) it MAY have
        /// the same id. Consumers MAY assume that Events with identical source and id are duplicates.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// REQUIRED. Identifies the context in which an event happened. Often this will include
        /// information such as the type of the event source, the organization publishing the event
        /// or the process that produced the event. The exact syntax and semantics behind the data
        /// encoded in the URI is defined by the event producer.
        /// </summary>
        public Uri Source { get; set; }

        /// <summary>
        /// The version of the CloudEvents specification which the event uses. This enables
        /// the interpretation of the context. Compliant event producers MUST use a value of 1.x-wip
        /// when referring to this version of the specification.
        /// </summary>
        public string SpecVersion => "1.0";

        /// <summary>
        /// REQUIRED. This attribute contains a value describing the type of event related to the
        /// originating occurrence. Often this attribute is used for routing, observability, policy
        /// enforcement, etc. The format of this is producer defined and might include information
        /// such as the version of the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Content type of data value.
        /// </summary>
        public ContentType DataContentType { get; set; }

        /// <summary>
        /// Identifies the schema that data adheres to. Incompatible changes to the schema SHOULD be
        /// reflected by a different URI.
        /// </summary>
        public Uri DataSchema { get; set; }

        /// <summary>
        /// This describes the subject of the event in the context of the event producer (identified
        /// by source). In publish-subscribe scenarios, a subscriber will typically subscribe to events
        /// emitted by a source, but the source identifier alone might not be sufficient as a qualifier
        /// for any specific event if the source context has internal sub-structure.
        /// 
        /// <para>Identifying the subject of the event in context metadata (opposed to only in the data
        /// payload) is particularly helpful in generic subscription filtering scenarios where middleware
        /// is unable to interpret the data content.In the above example, the subscriber might only be
        /// interested in blobs with names ending with '.jpg' or '.jpeg' and the subject attribute allows
        /// for constructing a simple and efficient string-suffix filter for that subset of events.</para>
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Timestamp of when the occurrence happened.
        /// </summary>
        public DateTime? Time { get; set; }

        /// <summary>
        /// Domain-specific information about the occurrence (i.e. the payload). This might include
        /// information about the occurrence, details about the data that was changed, or more.
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// Sets the data of the cloud event.
        /// </summary>
        /// <param name="data">The data of the cloud event.</param>
        public void SetData(string data) => Data = data;

        /// <summary>
        /// Sets the data of the cloud event.
        /// </summary>
        /// <param name="data">The data of the cloud event.</param>
        public void SetData(byte[] data) => Data = data;

        /// <summary>
        /// Creates a <see cref="SenderMessage"/> with headers mapped from the attributes of this cloud event.
        /// </summary>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map CloudEvent attributes to <see cref="SenderMessage"/>
        /// headers. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/> is used instead.
        /// </param>
        /// <returns>The mapped <see cref="SenderMessage"/>.</returns>
        public virtual SenderMessage ToSenderMessage(IProtocolBinding protocolBinding = null)
        {
            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            SenderMessage senderMessage;

            if (Data is string stringData)
                senderMessage = new SenderMessage(stringData);
            else if (Data is byte[] binaryData)
                senderMessage = new SenderMessage(binaryData);
            else
                senderMessage = new SenderMessage("");

            if (Id != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(IdHeader)] = Id;

            if (Source != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(SourceHeader)] = Source;

            senderMessage.Headers[protocolBinding.GetHeaderName(SpecVersionHeader)] = SpecVersion;

            if (Type != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(TypeHeader)] = Type;

            if (DataContentType != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(DataContentTypeHeader)] = DataContentType;

            if (DataSchema != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(DataSchemaHeader)] = DataSchema;

            if (Subject != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(SubjectHeader)] = Subject;

            if (Time != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(TimeHeader)] = Time.Value;

            return senderMessage;
        }

        /// <summary>
        /// Ensures that the required base cloud event attributes are present.
        /// </summary>
        /// <param name="senderMessage">The <see cref="SenderMessage"/> to validate.</param>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map CloudEvent attributes to <see cref="SenderMessage"/>
        /// headers. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/> is used instead.
        /// </param>
        protected static void ValidateCore(SenderMessage senderMessage, IProtocolBinding protocolBinding)
        {
            if (senderMessage is null)
                throw new ArgumentNullException(nameof(senderMessage));

            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            if (!HasHeaderOfType<string>(senderMessage, protocolBinding.GetHeaderName(IdHeader)))
                senderMessage.Headers[protocolBinding.GetHeaderName(IdHeader)] = Guid.NewGuid().ToString();

            if (!HasHeaderOfType<Uri>(senderMessage, protocolBinding.GetHeaderName(SourceHeader))
                && !HasHeaderOfType<string>(senderMessage, protocolBinding.GetHeaderName(SourceHeader)))
                throw new CloudEventValidationException($"The '{protocolBinding.GetHeaderName(SourceHeader)}' header is missing from the SenderMessage.");

            if (!HasHeaderOfType<string>(senderMessage, protocolBinding.GetHeaderName(TypeHeader)))
                throw new CloudEventValidationException($"The '{protocolBinding.GetHeaderName(TypeHeader)}' header is missing from the SenderMessage.");

            if (!HasHeaderOfType<DateTime>(senderMessage, protocolBinding.GetHeaderName(TimeHeader)))
                senderMessage.Headers[protocolBinding.GetHeaderName(TimeHeader)] = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="TCloudEvent"/> and initializes its base
        /// cloud event attributes according to the payload and headers of the
        /// <paramref name="receiverMessage"/>.
        /// </summary>
        /// <typeparam name="TCloudEvent">The type of cloud event to create.</typeparam>
        /// <param name="receiverMessage">
        /// The <see cref="IReceiverMessage"/> with headers that map to cloud event attributes.
        /// </param>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map <see cref="IReceiverMessage"/> headers to
        /// CloudEvent attributes. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/>
        /// is used instead.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="TCloudEvent"/> with its base cloud event attributes set.
        /// </returns>
        protected static TCloudEvent CreateCore<TCloudEvent>(IReceiverMessage receiverMessage, IProtocolBinding protocolBinding)
            where TCloudEvent : CloudEvent, new()
        {
            if (receiverMessage is null)
                throw new ArgumentNullException(nameof(receiverMessage));

            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            var cloudEvent = new TCloudEvent();

            if (receiverMessage.IsBinary())
                cloudEvent.SetData(receiverMessage.BinaryPayload);
            else
                cloudEvent.SetData(receiverMessage.StringPayload);

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(IdHeader), out string id))
                cloudEvent.Id = id;

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(SourceHeader), out Uri source))
                cloudEvent.Source = source;

            // SpecVersion?

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TypeHeader), out string type))
                cloudEvent.Type = type;

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataContentTypeHeader), out ContentType dataContentType))
                cloudEvent.DataContentType = dataContentType;
            else if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataContentTypeHeader), out string dataContentTypeString))
                cloudEvent.DataContentType = new ContentType(dataContentTypeString);

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataSchemaHeader), out Uri dataSchema))
                cloudEvent.DataSchema = dataSchema;

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(SubjectHeader), out string subject))
                cloudEvent.Subject = subject;

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TimeHeader), out DateTime time))
                cloudEvent.Time = time;
            else if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TimeHeader), out string timeString))
                cloudEvent.Time = DateTime.Parse(timeString);

            return cloudEvent;
        }

        private static bool HasHeaderOfType<T>(SenderMessage senderMessage, string headerName) =>
            senderMessage.Headers.TryGetValue(headerName, out var value)
                && value is T;
    }
}
