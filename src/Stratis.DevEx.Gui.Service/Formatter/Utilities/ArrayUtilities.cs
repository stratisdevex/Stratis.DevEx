namespace Stratis.DevEx.Formatters
{
    /// <summary>
    /// Full Array support for net40.
    /// <![CDATA[Version: 1.0.0.0]]> <br/>
    /// </summary>
    public static class ArrayUtilities
    {
        /// <summary>
        /// System.Array.Empty or new T[0].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] Empty<T>()
        {

            return System.Array.Empty<T>();

        }
    }
}
