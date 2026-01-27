// C:\Projects\RotaAppMobile\src\screens\auth\LoginScreen.tsx

import React, { useState } from 'react';
import {
  View,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Image,  // Image burada import edilmiş
  Alert,
} from 'react-native';
import {
  TextInput,
  Button,
  Text,
  Snackbar,
  Surface,
  Headline,
  Subheading,
} from 'react-native-paper';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useAuth } from '../../contexts/AuthContext';
import { LoginRequest } from '../../types/auth.types';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';

const schema = yup.object({
  email: yup
    .string()
    .email('Geçerli bir email adresi girin')
    .required('Email zorunludur'),
  password: yup
    .string()
    .min(6, 'Şifre en az 6 karakter olmalıdır')
    .required('Şifre zorunludur'),
});

const LoginScreen: React.FC = () => {
  const { login } = useAuth();
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [snackbarVisible, setSnackbarVisible] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginRequest>({
    resolver: yupResolver(schema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data: LoginRequest) => {
    setLoading(true);
    try {
      await login(data);
    } catch (error: any) {
      const errorMessage = error.userFriendlyMessage || error.message || 'Giriş işlemi başarısız oldu';
      setSnackbarMessage(errorMessage);
      setSnackbarVisible(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      style={styles.container}
    >
      <ScrollView
        contentContainerStyle={styles.scrollContainer}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.logoContainer}>
          {/* YolPilot Logo */}
          <Image 
            source={require('../../../assets/images/yolpilot-logo.png')}
            style={styles.logo}
            resizeMode="contain"
          />
          <Text style={styles.logoText}>YolPilot</Text>
          <Headline style={styles.title}>Hoş Geldiniz</Headline>
          <Subheading style={styles.subtitle}>
            Devam etmek için giriş yapın
          </Subheading>
        </View>

        <Surface style={styles.formContainer} elevation={2}>
          <Controller
            control={control}
            name="email"
            render={({ field: { onChange, onBlur, value } }) => (
              <TextInput
                label="Email"
                value={value}
                onChangeText={onChange}
                onBlur={onBlur}
                error={!!errors.email}
                mode="outlined"
                keyboardType="email-address"
                autoCapitalize="none"
                autoComplete="email"
                left={<TextInput.Icon icon="email" />}
                style={styles.input}
              />
            )}
          />
          {errors.email && (
            <Text style={styles.errorText}>{errors.email.message}</Text>
          )}

          <Controller
            control={control}
            name="password"
            render={({ field: { onChange, onBlur, value } }) => (
              <TextInput
                label="Şifre"
                value={value}
                onChangeText={onChange}
                onBlur={onBlur}
                error={!!errors.password}
                mode="outlined"
                secureTextEntry={!showPassword}
                autoCapitalize="none"
                autoComplete="password"
                left={<TextInput.Icon icon="lock" />}
                right={
                  <TextInput.Icon
                    icon={showPassword ? 'eye-off' : 'eye'}
                    onPress={() => setShowPassword(!showPassword)}
                  />
                }
                style={styles.input}
              />
            )}
          />
          {errors.password && (
            <Text style={styles.errorText}>{errors.password.message}</Text>
          )}

          <Button
            mode="contained"
            onPress={handleSubmit(onSubmit)}
            loading={loading}
            disabled={loading}
            style={styles.button}
            contentStyle={styles.buttonContent}
          >
            {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
          </Button>

          <Button
            mode="text"
            onPress={() => Alert.alert('Bilgi', 'Şifre sıfırlama özelliği yakında eklenecek')}
            style={styles.forgotButton}
          >
            Şifremi Unuttum
          </Button>
        </Surface>

        <View style={styles.footer}>
          <Text style={styles.footerText}>
            Hesabınız yok mu?{' '}
            <Text
              style={styles.linkText}
              onPress={() => Alert.alert(
                'Kayıt İşlemi', 
                'Yeni kullanıcı kayıtları web panelinizden yapılmaktadır.\n\nLütfen yöneticiniz ile iletişime geçin veya https://app.yolpilot.com adresinden kayıt olun.',
                [
                  { text: 'Tamam', style: 'default' }
                ]
              )}
            >
              Kayıt Olun
            </Text>
          </Text>
        </View>
      </ScrollView>

      <Snackbar
        visible={snackbarVisible}
        onDismiss={() => setSnackbarVisible(false)}
        duration={3000}
        action={{
          label: 'Tamam',
          onPress: () => setSnackbarVisible(false),
        }}
      >
        {snackbarMessage}
      </Snackbar>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  scrollContainer: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 20,
  },
  logoContainer: {
    alignItems: 'center',
    marginBottom: 30,
  },
  logo: {
    width: 150,
    height: 80,
    marginBottom: 10,
  },
  logoText: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#2C3E50',
    marginBottom: 10,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
  },
  formContainer: {
    padding: 20,
    borderRadius: 10,
    backgroundColor: 'white',
  },
  input: {
    marginBottom: 10,
  },
  errorText: {
    color: '#d32f2f',
    fontSize: 12,
    marginBottom: 10,
    marginLeft: 5,
  },
  button: {
    marginTop: 20,
    borderRadius: 25,
  },
  buttonContent: {
    paddingVertical: 5,
  },
  forgotButton: {
    marginTop: 10,
  },
  footer: {
    marginTop: 30,
    alignItems: 'center',
  },
  footerText: {
    fontSize: 14,
    color: '#666',
  },
  linkText: {
    color: '#6200ee',
    fontWeight: 'bold',
  },
});

export default LoginScreen;