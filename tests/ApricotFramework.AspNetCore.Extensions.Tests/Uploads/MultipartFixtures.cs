namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

/// <summary>
/// Multipart request bodies captured off the wire, byte for byte, from requests made by
/// curl 8.7.1 to a raw netcat listener.
/// </summary>
/// <remarks>
/// The point of capturing rather than generating these is that curl is an implementation this
/// library had no hand in. A fixture written by the same code it verifies would agree with any
/// bug that code has. Stored base64 so no line-ending normalisation can touch the CRLF the
/// multipart grammar depends on.
/// </remarks>
internal static class MultipartFixtures
{
    /// <summary>
    /// One captured request body and the content type that describes it.
    /// </summary>
    /// <param name="ContentType">The full request content type, carrying the boundary.</param>
    /// <param name="Base64Body">The request body, base64 encoded.</param>
    internal sealed record Fixture(string ContentType, string Base64Body)
    {
        /// <summary>
        /// The raw body bytes.
        /// </summary>
        /// <returns>A fresh copy of the decoded body.</returns>
        public byte[] GetBody() => Convert.FromBase64String(this.Base64Body);
    }

    /// <summary>
    /// A single file field, no accompanying form fields.
    /// </summary>
    public static Fixture OneFile => new(
        "multipart/form-data; boundary=------------------------ELNHyaCx2lvo4dhVGpGUm8",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1FTE5IeWFDeDJsdm80ZGhWR3BHVW04DQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImZpbGUiOyBmaWxlbmFtZT0ic21hbGwudHh0Ig0KQ29udGVudC1UeXBlOiB0ZXh0L3BsYWluDQoNCmhlbGxvIHdvcmxkDQotLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLUVMTkh5YUN4Mmx2bzRkaFZHcEdVbTgtLQ0K");

    /// <summary>
    /// Two file fields in one request.
    /// </summary>
    public static Fixture TwoFiles => new(
        "multipart/form-data; boundary=------------------------gE4EJCoZ4jxrXNqmbrEv7n",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1nRTRFSkNvWjRqeHJYTnFtYnJFdjduDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImZpbGUxIjsgZmlsZW5hbWU9InNtYWxsLnR4dCINCkNvbnRlbnQtVHlwZTogdGV4dC9wbGFpbg0KDQpoZWxsbyB3b3JsZA0KLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1nRTRFSkNvWjRqeHJYTnFtYnJFdjduDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImZpbGUyIjsgZmlsZW5hbWU9InNlY29uZC50eHQiDQpDb250ZW50LVR5cGU6IHRleHQvcGxhaW4NCg0KbGluZSBvbmUKbGluZSB0d28KDQotLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLWdFNEVKQ29aNGp4clhOcW1ickV2N24tLQ0K");

    /// <summary>
    /// Files and fields together, including a repeated field key.
    /// </summary>
    public static Fixture Mixed => new(
        "multipart/form-data; boundary=------------------------muvxvBiXegBhaSzLjbQI0s",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1tdXZ4dkJpWGVnQmhhU3pMamJRSTBzDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9InRpdGxlIg0KDQpSZXBvcnQNCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tbXV2eHZCaVhlZ0JoYVN6TGpiUUkwcw0KQ29udGVudC1EaXNwb3NpdGlvbjogZm9ybS1kYXRhOyBuYW1lPSJ0YWciDQoNCmENCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tbXV2eHZCaVhlZ0JoYVN6TGpiUUkwcw0KQ29udGVudC1EaXNwb3NpdGlvbjogZm9ybS1kYXRhOyBuYW1lPSJ0YWciDQoNCmINCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tbXV2eHZCaVhlZ0JoYVN6TGpiUUkwcw0KQ29udGVudC1EaXNwb3NpdGlvbjogZm9ybS1kYXRhOyBuYW1lPSJmaWxlIjsgZmlsZW5hbWU9InNtYWxsLnR4dCINCkNvbnRlbnQtVHlwZTogdGV4dC9wbGFpbg0KDQpoZWxsbyB3b3JsZA0KLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1tdXZ4dkJpWGVnQmhhU3pMamJRSTBzLS0NCg==");

    /// <summary>
    /// Two fields and no files. Neither section declares a charset.
    /// </summary>
    public static Fixture FieldsOnly => new(
        "multipart/form-data; boundary=------------------------1puZK3PdlrX2xn2nJAvpsO",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0xcHVaSzNQZGxyWDJ4bjJuSkF2cHNPDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImFscGhhIg0KDQpvbmUNCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tMXB1WkszUGRsclgyeG4ybkpBdnBzTw0KQ29udGVudC1EaXNwb3NpdGlvbjogZm9ybS1kYXRhOyBuYW1lPSJiZXRhIg0KDQp0d28NCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tMXB1WkszUGRsclgyeG4ybkpBdnBzTy0tDQo=");

    /// <summary>
    /// One field declaring 'text/plain; charset=utf-8'.
    /// </summary>
    public static Fixture CharsetField => new(
        "multipart/form-data; boundary=------------------------4E3TelxH1HybNVraffeHZ4",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS00RTNUZWx4SDFIeWJOVnJhZmZlSFo0DQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9Im5vdGUiDQpDb250ZW50LVR5cGU6IHRleHQvcGxhaW47IGNoYXJzZXQ9dXRmLTgNCg0KY2Fmw6kNCi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tNEUzVGVseEgxSHliTlZyYWZmZUhaNC0tDQo=");

    /// <summary>
    /// A file whose name and content are both non-ASCII.
    /// </summary>
    public static Fixture UnicodeFile => new(
        "multipart/form-data; boundary=------------------------cYNTbv6QAWD1NMgRk5876f",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS1jWU5UYnY2UUFXRDFOTWdSazU4NzZmDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImZpbGUiOyBmaWxlbmFtZT0idW5pY29kZS1jYWbDqS50eHQiDQpDb250ZW50LVR5cGU6IHRleHQvcGxhaW4NCg0KY2Fmw6kgbmHDr3ZlDQotLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLWNZTlRidjZRQVdEMU5NZ1JrNTg3NmYtLQ0K");

    /// <summary>
    /// A file section first, then two fields, so a field can only be reached by draining the file.
    /// </summary>
    public static Fixture FileThenField => new(
        "multipart/form-data; boundary=------------------------4xsIwiNz5NDkAZ6qKAy75U",
        "LS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS00eHNJd2lOejVORGtBWjZxS0F5NzVVDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9ImZpbGUiOyBmaWxlbmFtZT0ic21hbGwudHh0Ig0KQ29udGVudC1UeXBlOiB0ZXh0L3BsYWluDQoNCmhlbGxvIHdvcmxkDQotLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLTR4c0l3aU56NU5Ea0FaNnFLQXk3NVUNCkNvbnRlbnQtRGlzcG9zaXRpb246IGZvcm0tZGF0YTsgbmFtZT0idGl0bGUiDQoNClJlcG9ydA0KLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS00eHNJd2lOejVORGtBWjZxS0F5NzVVDQpDb250ZW50LURpc3Bvc2l0aW9uOiBmb3JtLWRhdGE7IG5hbWU9InRhZyINCg0Keg0KLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS00eHNJd2lOejVORGtBWjZxS0F5NzVVLS0NCg==");
}
