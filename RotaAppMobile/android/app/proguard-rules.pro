# Add project specific ProGuard rules here.
# By default, the flags in this file are appended to flags specified
# in /usr/local/Cellar/android-sdk/24.3.3/tools/proguard/proguard-android.txt
# You can edit the include path and order by changing the proguardFiles
# directive in build.gradle.
#
# For more details, see
#   http://developer.android.com/guide/developing/tools/proguard.html

# Add any project specific keep options here:

# React Native
-keep class com.facebook.react.** { *; }
-keep class com.facebook.hermes.reactexecutor.** { *; }
-keep class com.facebook.jni.** { *; }

# React Native Paper
-keep class com.google.android.material.** { *; }

# Vector Icons
-keep class com.oblador.vectoricons.** { *; }

# Keep React Native classes
-keep class com.facebook.react.ReactActivity { *; }
-keep class com.facebook.react.ReactApplication { *; }
-keep class com.facebook.react.ReactNativeHost { *; }
-keep class com.facebook.react.bridge.** { *; }
-keep class com.facebook.react.modules.** { *; }
-keep class com.facebook.react.views.** { *; }

# Keep native modules
-keep class com.yolpilot.driver.** { *; }
