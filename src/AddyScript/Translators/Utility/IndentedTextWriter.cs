using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace AddyScript.Translators.Utility;


public class IndentedTextWriter(TextWriter innerWriter) : TextWriter
{
    private readonly TextWriter innerWriter = innerWriter;
    private readonly Stack<string> prefixStack = new();
    private readonly StringBuilder prefixBuilder = new();
    private string linePrefix = string.Empty;
    private bool atLineStart = true;

    public int Indentation { get; set; }

    public void PushPrefix()
    {
        linePrefix += prefixBuilder.ToString();
        prefixStack.Push(linePrefix);
    }

    public void PopPrefix()
    {
        prefixStack.Pop();
        linePrefix = prefixStack.Count > 0 ? prefixStack.Peek() : string.Empty;
    }

    #region Overrides of TextWriter

    /// <summary>
    /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
    /// </summary>
    /// <filterpriority>1</filterpriority>
    public override void Flush() => innerWriter.Flush();

    /// <summary>
    /// Closes the current writer and releases any system resources associated with the writer.
    /// </summary>
    /// <filterpriority>1</filterpriority>
    public override void Close() => innerWriter.Close();

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="T:System.IO.TextWriter" /> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            innerWriter.Dispose();
    }

    /// <summary>
    /// Writes a character to the text stream.
    /// </summary>
    /// <param name="value">The character to write to the text stream. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(char value)
    {
        if (atLineStart)
        {
            for (int i = 0; i < Indentation; ++i)
                innerWriter.Write('\t');

            innerWriter.Write(linePrefix);
        }

        innerWriter.Write(value);
        atLineStart = value == '\n';

        if (atLineStart)
            prefixBuilder.Clear();
        else
            prefixBuilder.Append(value is ' ' or '\t' ? value : ' ');
    }

    /// <summary>
    /// Writes a character array to the text stream.
    /// </summary>
    /// <param name="buffer">The character array to write to the text stream. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(char[] buffer)
    {
        foreach (char c in buffer)
            Write(c);
    }

    /// <summary>
    /// Writes a subarray of characters to the text stream.
    /// </summary>
    /// <param name="buffer">The character array to write data from. </param>
    /// <param name="index">Starting index in the buffer. </param>
    /// <param name="count">The number of characters to write. </param>
    /// <exception cref="ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />. </exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="buffer" /> parameter is null. </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> or <paramref name="count" /> is negative. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(char[] buffer, int index, int count)
    {
        for (int i = 0, j = index; i < count && j < buffer.Length; ++i, ++j)
            Write(buffer[j]);
    }

    /// <summary>
    /// Writes the text representation of a Boolean value to the text stream.
    /// </summary>
    /// <param name="value">The Boolean to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(bool value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of a 4-byte signed integer to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte signed integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(int value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of a 4-byte unsigned integer to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte unsigned integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(uint value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of an 8-byte signed integer to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte signed integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(long value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of an 8-byte unsigned integer to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte unsigned integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(ulong value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of a 4-byte floating-point value to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte floating-point value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(float value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of an 8-byte floating-point value to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte floating-point value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(double value) => Write(value.ToString());

    /// <summary>
    /// Writes the text representation of a decimal value to the text stream.
    /// </summary>
    /// <param name="value">The decimal value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(decimal value) => Write(value.ToString());

    /// <summary>
    /// Writes a string to the text stream.
    /// </summary>
    /// <param name="value">The string to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(string value)
    {
        foreach (char c in value)
            Write(c);
    }

    /// <summary>
    /// Writes the text representation of an object to the text stream by calling ToString on that object.
    /// </summary>
    /// <param name="value">The object to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void Write(object value) => Write(value.ToString());

    /// <summary>
    /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg0">An object to write into the formatted string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void Write(string format, object arg0) =>
        Write(string.Format(format, arg0));

    /// <summary>
    /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg0">An object to write into the formatted string. </param>
    /// <param name="arg1">An object to write into the formatted string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void Write(string format, object arg0, object arg1) =>
        Write(string.Format(format, arg0, arg1));

    /// <summary>
    /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg0">An object to write into the formatted string. </param>
    /// <param name="arg1">An object to write into the formatted string. </param>
    /// <param name="arg2">An object to write into the formatted string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void Write(string format, object arg0, object arg1, object arg2) =>
        Write(string.Format(format, arg0, arg1, arg2));

    /// <summary>
    /// Writes out a formatted string, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg">The object array to write into the formatted string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> or <paramref name="arg" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to <paramref name="arg" />. Length. </exception><filterpriority>1</filterpriority>
    public override void Write(string format, object[] arg) => Write(string.Format(format, arg));

    /// <summary>
    /// Writes a line terminator to the text stream.
    /// </summary>
    /// <returns>
    /// The default line terminator is a carriage return followed by a line feed ("\r\n"), but this value can be changed using the <see cref="P:System.IO.TextWriter.NewLine" /> property.
    /// </returns>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine() => Write(NewLine);

    /// <summary>
    /// Writes a character followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The character to write to the text stream. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes an array of characters followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="buffer">The character array from which data is read. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(char[] buffer)
    {
        Write(buffer);
        WriteLine();
    }

    /// <summary>
    /// Writes a subarray of characters followed by a line terminator to the text stream.
    /// </summary>
    /// <returns>
    /// Characters are read from <paramref name="buffer" /> beginning at <paramref name="index" /> and ending at <paramref name="index" /> + <paramref name="count" />.
    /// </returns>
    /// <param name="buffer">The character array from which data is read. </param>
    /// <param name="index">The index into <paramref name="buffer" /> at which to begin reading. </param>
    /// <param name="count">The maximum number of characters to write. </param>
    /// <exception cref="ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />. </exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="buffer" /> parameter is null. </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> or <paramref name="count" /> is negative. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a Boolean followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The Boolean to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(bool value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a 4-byte signed integer followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte signed integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(int value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a 4-byte unsigned integer followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte unsigned integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(uint value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of an 8-byte signed integer followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte signed integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(long value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of an 8-byte unsigned integer followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte unsigned integer to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(ulong value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a 4-byte floating-point value followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 4-byte floating-point value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(float value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a 8-byte floating-point value followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The 8-byte floating-point value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(double value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of a decimal value followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The decimal value to write. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(decimal value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes a string followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The string to write. If <paramref name="value" /> is null, only the line termination characters are written. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(string value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes the text representation of an object by calling ToString on this object, followed by a line terminator to the text stream.
    /// </summary>
    /// <param name="value">The object to write. If <paramref name="value" /> is null, only the line termination characters are written. </param>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(object value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatted string. </param>
    /// <param name="arg0">The object to write into the formatted string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(string format, object arg0)
    {
        Write(format, arg0);
        WriteLine();
    }

    /// <summary>
    /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg0">The object to write into the format string. </param>
    /// <param name="arg1">The object to write into the format string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(string format, object arg0, object arg1)
    {
        Write(format, arg0, arg1);
        WriteLine();
    }

    /// <summary>
    /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg0">The object to write into the format string. </param>
    /// <param name="arg1">The object to write into the format string. </param>
    /// <param name="arg2">The object to write into the format string. </param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="format" /> is null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to the number of provided objects to be formatted. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(string format, object arg0, object arg1, object arg2)
    {
        Write(format, arg0, arg1, arg2);
        WriteLine();
    }

    /// <summary>
    /// Writes out a formatted string and a new line, using the same semantics as <see cref="M:System.String.Format(System.String,System.Object)" />.
    /// </summary>
    /// <param name="format">The formatting string. </param>
    /// <param name="arg">The object array to write into format string. </param>
    /// <exception cref="T:System.ArgumentNullException">A string or object is passed in as null. </exception>
    /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter" /> is closed. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <exception cref="T:System.FormatException">The format specification in format is invalid.-or- The number indicating an argument to be formatted is less than zero, or larger than or equal to arg.Length. </exception><filterpriority>1</filterpriority>
    public override void WriteLine(string format, object[] arg)
    {
        Write(format, arg);
        WriteLine();
    }

    /// <summary>
    /// Gets an object that controls formatting.
    /// </summary>
    /// <returns>
    /// An <see cref="IFormatProvider" /> object for a specific culture, or the formatting of the current culture if no other culture is specified.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public override IFormatProvider FormatProvider => innerWriter.FormatProvider;

    /// <summary>
    /// When overridden in a derived class, returns the <see cref="T:System.Text.Encoding" /> in which the output is written.
    /// </summary>
    /// <returns>
    /// The Encoding in which the output is written.
    /// </returns>
    /// <filterpriority>1</filterpriority>
    public override Encoding Encoding => innerWriter.Encoding;

    /// <summary>
    /// Gets or sets the line terminator string used by the current TextWriter.
    /// </summary>
    /// <returns>
    /// The line terminator string for the current TextWriter.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public override string NewLine
    {
        get => innerWriter.NewLine;
        set => innerWriter.NewLine = value;
    }

    #endregion
}