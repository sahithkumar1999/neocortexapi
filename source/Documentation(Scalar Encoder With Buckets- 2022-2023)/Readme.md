 Group Name: Team_SpiralGanglions

Abstract:
In many machine learning applications, encoding data is an important step in preparing the data for analysis. The Scalar Encoder is a commonly used method 
for encoding numerical data, but it has limitations in terms of precision and flexibility. In this project, we implemented an improved version of the Scalar 
Encoder called the Scalar Encoder with Buckets. This method maps input values to continuous ranges of buckets, providing better precision in encoding data 
compared to the Scalar Encoder. It also automatically sets parameters based on the input data and supports periodic encoding of values, increasing the flexibility 
of the encoding process. We evaluated the performance of the Scalar Encoder with Buckets on various datasets and compared it to the Scalar Encoder, demonstrating 
its superior performance in terms of precision and flexibility.

Introduction:
Data encoding is an important step in many machine learning applications, as it allows for efficient storage and analysis of data. The Scalar Encoder is a commonly 
used method for encoding numerical data, but it has limitations in terms of precision and flexibility. The Scalar Encoder maps input values to individual buckets, 
which can result in loss of precision when encoding data. Additionally, the Scalar Encoder requires manual specification of parameters such as the number of buckets 
and bucket size, which can make it inflexible and difficult to adapt to different datasets.

Methodology:
To overcome the limitations of the Scalar Encoder, we implemented an improved version called the Scalar Encoder with Buckets. This method maps input values to 
continuous ranges of buckets, providing better precision in encoding data compared to the Scalar Encoder. The ClosenessScores method calculates the closeness score 
of an input value to each bucket range, indicating the level of activation for each bucket range. This provides a more precise encoding of input data compared to the 
Scalar Encoder, where the input values are mapped to individual buckets.

In addition to improved precision, the Scalar Encoder with Buckets also offers increased flexibility. The method automatically sets parameters such as the number of 
buckets and bucket size based on the input data, making it more flexible and easier to adapt to different datasets. The GenerateRangeDescription method generates a 
description of the bucket ranges used to encode the input data, which can help users understand the encoding scheme and adjust the parameters as needed. 
This increases the flexibility of the Scalar Encoder with Buckets and makes it easier to adapt to different datasets.

The Scalar Encoder with Buckets also supports periodic encoding of values, allowing for better handling of cyclical data. The GetTopDownMapping method generates a 
mapping of the bucket ranges to a hierarchy of levels, which can be useful for representing cyclical data such as time of day or day of the week. The GetBucketValues 
method returns the list of bucket values for a given bucket range, allowing users to encode cyclical data using a periodic encoding scheme.

Results:
We evaluated the performance of the Scalar Encoder with Buckets on various datasets and compared it to the Scalar Encoder. Our results showed that the Scalar 
Encoder with Buckets outperformed the Scalar Encoder in terms of precision and flexibility. The improved encoding scheme of the Scalar Encoder with Buckets 
provided better precision in encoding data compared to the Scalar Encoder, while the automatic setting of parameters and support for periodic encoding made 
it more flexible and easier to adapt to different datasets.

Conclusion:
In conclusion, the Scalar Encoder with Buckets is an improved version of the Scalar Encoder that overcomes some of its limitations. It provides better precision 
in encoding data by mapping input values to continuous ranges of buckets, and supports periodic encoding of values for better handling of cyclical data. The automatic
setting of parameters based on the input data, as well as the generation of a bucket range description, increases the flexibility of the Scalar Encoder with Buckets 
and makes it easier to adapt to different datasets.

Work done in this project
Implemented the ClosenessScores method that calculates the similarity score between two values based on the overlap of their encoding bit arrays.
Developed the EncodeIntoArray method that encodes a given value into an array of bits using the scalar encoder algorithm.
Created the GetBucketIndex method that maps a scalar value to a specific bucket based on its encoded bit array.
Implemented the GenerateRangeDescription method that generates a human-readable description of a range of scalar values based on their encoding bit arrays.
Developed the GetBucketValues method that returns the scalar values associated with a specific bucket based on its encoded bit array.
Wrote comprehensive unit tests for each of the above methods to ensure their correctness and robustness.
Followed best practices for code organization, including using meaningful function and variable names and separating concerns into different modules.
Used appropriate data structures and algorithms to optimize the performance and memory usage of the methods.
Followed the established coding conventions and style guidelines of the project to ensure consistency and readability of the code.
we are currently Documenting each method with clear and concise comments explaining its purpose, input parameters, output, and any assumptions or limitations.
We will upload the paper and video links as soon as possible, If you could take the time to review the code and provide me with feedback on areas that need improvement or revision, I would be very grateful. Any suggestions you have for changes or optimizations would be greatly appreciated.

Thank you very much for your time and consideration. I look forward to hearing from you soon.


Project Title
ML22/23-1 Scalar Encoder with Buckets

[Main Branch]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/Team_SpiralGanglions), But we had faced some problems in test cases so we continued the work in [another Branch](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/test-cases) --> fully working code is there in this branch


 Program Links:
  Link for [Scalar Encoder](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/NeoCortexApi/Encoders/ScalarEncoder.cs)
  Link for [EncoderBaseClass](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/NeoCortexApi/Encoders/EncoderBase.cs )
  Link for [Test Cases]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/UnitTestsProject/EncoderTests/ScalarEncoderTests.cs)




Link to project :
 Program links for different methods
    [ClosenessScores ](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L701 )
    [EncodeIntoArray ](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L346 )
    [decode ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L241)
    [ GetBucketIndex](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L475)
    [GetTopDownMapping ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L817)
    [GenerateRangeDescription ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L599 )
    [GetBucketValues ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L769 )
    [GetBucketInfo](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/3a2546872cbd9d96a7233fdd8125a1a7053b45b4/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L527)

Forked from:
https://github.com/ddobric/neocortexapi

 Team Contribution link
       [Singari Sahith Kumar](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/Singari-Sahith-Kumar?author=sahithkumar1999 ) 
      [Anil Kumar Gadiraju](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/Anilkumar?author=anilkumargadirajuFraUas)
      [Vinay Kumar Bandaru](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/vinay-kumar-bandaru?author=Vinaykumarbandaru1999 )

   Partially completed [Documentation](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/test-cases/source/Documentation(Scalar%20Encoder%20With%20Buckets-%202022-2023))

   Link for test cases code [Click Here](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/UnitTestsProject/EncoderTests/ScalarEncoderTests.cs)




Test cases were passed
![image](https://user-images.githubusercontent.com/64829519/227833329-f7b5ef39-2cea-4de4-88a8-082a0b386775.png)

Periodic and Non-periodic test cases outputs for the method GetBucketIndex
![image](https://user-images.githubusercontent.com/64829519/227833634-ec274d8f-0046-41e6-a11a-3fac5363ccc4.png)

