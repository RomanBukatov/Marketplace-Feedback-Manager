import React, { useState } from 'react';
import { Button, Card, Form, Input, message, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { authService } from '../api/services';

const { Title } = Typography;

interface LoginValues {
  password: string;
}

const LoginPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const onFinish = async (values: LoginValues) => {
    try {
      setLoading(true);
      await authService.login(values.password);
      message.success('Добро пожаловать!');
      navigate('/');
    } catch (error) {
      message.error('Неверный пароль');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      height: '100vh',
      background: '#f0f2f5'
    }}>
      <Card style={{ width: 400, boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div style={{ fontSize: 48, marginBottom: 8 }}>🔐</div>
          <Title level={3}>Вход в систему</Title>
        </div>
        
        <Form
          name="login"
          onFinish={onFinish}
          size="large"
        >
          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Введите пароль администратора' }]}
          >
            <Input.Password 
              prefix={<LockOutlined />}
              placeholder="Пароль администратора"
            />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" block loading={loading}>
              Войти
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPage;