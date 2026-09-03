using System;

namespace Baion.Contracts;

/// <summary>Violación del protocolo por el otro extremo: trama malformada o por encima del tamaño permitido.</summary>
public class BaionProtocolException(string message) : Exception(message);
