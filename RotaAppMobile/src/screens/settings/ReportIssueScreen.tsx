// C:\Projects\RotaAppMobile\src\screens\settings\ReportIssueScreen.tsx

import React, { useState } from 'react';
import { View, ScrollView, StyleSheet, Alert, KeyboardAvoidingView, Platform } from 'react-native';
import { 
  Card, 
  TextInput, 
  Button, 
  Text, 
  RadioButton,
  HelperText,
  Chip
} from 'react-native-paper';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import DeviceInfo from 'react-native-device-info';

const ReportIssueScreen: React.FC = () => {
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [issueType, setIssueType] = useState('bug');
  const [subject, setSubject] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<any>({});

  const issueTypes = [
    { value: 'bug', label: 'Hata/Bug', icon: 'bug' },
    { value: 'feature', label: 'Özellik İsteği', icon: 'lightbulb' },
    { value: 'performance', label: 'Performans Sorunu', icon: 'speedometer' },
    { value: 'other', label: 'Diğer', icon: 'help-circle' }
  ];

  const validate = () => {
    const newErrors: any = {};
    
    if (!subject.trim()) {
      newErrors.subject = 'Konu zorunludur';
    } else if (subject.length < 5) {
      newErrors.subject = 'Konu en az 5 karakter olmalıdır';
    }
    
    if (!description.trim()) {
      newErrors.description = 'Açıklama zorunludur';
    } else if (description.length < 20) {
      newErrors.description = 'Açıklama en az 20 karakter olmalıdır';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    setLoading(true);
    try {
      // Cihaz bilgilerini topla
      const deviceInfo = {
        platform: Platform.OS,
        version: Platform.Version,
        appVersion: DeviceInfo.getVersion(),
        buildNumber: DeviceInfo.getBuildNumber(),
        deviceModel: await DeviceInfo.getModel(),
        deviceBrand: await DeviceInfo.getBrand(),
      };

      const payload = {
        issueType,
        subject,
        description,
        reportedBy: user?.email,
        reportedByName: user?.fullName,
        workspaceId: user?.workspaceId,
        deviceInfo: JSON.stringify(deviceInfo),
        status: 'Open',
        createdAt: new Date().toISOString()
      };

      await api.post('/workspace/issues/report', payload);

      Alert.alert(
        'Başarılı',
        'Sorun bildiriminiz alındı. En kısa sürede inceleyip size dönüş yapacağız.',
        [
          {
            text: 'Tamam',
            onPress: () => {
              // Formu temizle
              setSubject('');
              setDescription('');
              setIssueType('bug');
            }
          }
        ]
      );
    } catch (error: any) {
      Alert.alert(
        'Hata',
        error.response?.data?.message || 'Sorun bildirimi gönderilemedi. Lütfen tekrar deneyin.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView 
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView>
        <Card style={styles.card}>
          <Card.Content>
            <Text variant="titleLarge" style={styles.title}>
              Sorun Bildir
            </Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
              Yaşadığınız sorunu veya önerinizi bize bildirin
            </Text>

            {/* Sorun Tipi */}
            <View style={styles.section}>
              <Text variant="titleMedium" style={styles.sectionTitle}>
                Sorun Tipi
              </Text>
              <View style={styles.chipContainer}>
                {issueTypes.map((type) => (
                  <Chip
                    key={type.value}
                    icon={type.icon}
                    selected={issueType === type.value}
                    onPress={() => setIssueType(type.value)}
                    style={styles.chip}
                    selectedColor="#3B82F6"
                  >
                    {type.label}
                  </Chip>
                ))}
              </View>
            </View>

            {/* Konu */}
            <View style={styles.section}>
              <TextInput
                label="Konu *"
                value={subject}
                onChangeText={(text) => {
                  setSubject(text);
                  if (errors.subject) {
                    setErrors({ ...errors, subject: null });
                  }
                }}
                mode="outlined"
                error={!!errors.subject}
                placeholder="Sorunu kısaca özetleyin"
              />
              {errors.subject && (
                <HelperText type="error" visible={true}>
                  {errors.subject}
                </HelperText>
              )}
            </View>

            {/* Açıklama */}
            <View style={styles.section}>
              <TextInput
                label="Açıklama *"
                value={description}
                onChangeText={(text) => {
                  setDescription(text);
                  if (errors.description) {
                    setErrors({ ...errors, description: null });
                  }
                }}
                mode="outlined"
                multiline
                numberOfLines={6}
                error={!!errors.description}
                placeholder="Sorunu detaylı olarak açıklayın. Ne zaman oluştu? Hangi işlemi yaparken? Hata mesajı var mı?"
                style={styles.textArea}
              />
              {errors.description && (
                <HelperText type="error" visible={true}>
                  {errors.description}
                </HelperText>
              )}
            </View>

            {/* Gönderen Bilgisi */}
            <View style={styles.infoSection}>
              <Text variant="bodySmall" style={styles.infoText}>
                Gönderen: {user?.fullName} ({user?.email})
              </Text>
              <Text variant="bodySmall" style={styles.infoText}>
                Cihaz: {Platform.OS === 'ios' ? 'iOS' : 'Android'} {Platform.Version}
              </Text>
            </View>

            {/* Gönder Butonu */}
            <Button
              mode="contained"
              onPress={handleSubmit}
              loading={loading}
              disabled={loading}
              style={styles.submitButton}
              icon="send"
            >
              Gönder
            </Button>
          </Card.Content>
        </Card>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  card: {
    margin: 10,
  },
  title: {
    fontWeight: 'bold',
    marginBottom: 5,
  },
  subtitle: {
    color: '#666',
    marginBottom: 20,
  },
  section: {
    marginBottom: 20,
  },
  sectionTitle: {
    marginBottom: 10,
    fontWeight: '600',
  },
  chipContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    marginRight: 8,
    marginBottom: 8,
  },
  textArea: {
    minHeight: 120,
  },
  infoSection: {
    backgroundColor: '#f0f0f0',
    padding: 10,
    borderRadius: 5,
    marginBottom: 20,
  },
  infoText: {
    color: '#666',
    marginBottom: 2,
  },
  submitButton: {
    marginTop: 10,
  },
});

export default ReportIssueScreen;